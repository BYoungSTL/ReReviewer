import type { Metadata } from "next";
import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { Suspense, type ReactNode } from "react";
import { buttonVariants } from "@/components/ui/button";
import { failed, userApi } from "@/lib/api/client";
import { getReviewer, isGuid } from "@/lib/api/public";
import { claimReviewer } from "@/lib/requests/actions";
import { ClaimForm } from "./claim-form";

export const metadata: Metadata = {
  title: "Привязать профиль",
  robots: { index: false },
};

export default function ClaimPage({ params }: PageProps<"/reviewers/[id]/claim">) {
  return (
    <Suspense fallback={<p className="text-muted-foreground">Загружаем…</p>}>
      <Claim params={params} />
    </Suspense>
  );
}

// Запрос можно отправить, только если ни профиль, ни аккаунт ещё не привязаны и другой заявки на себя нет.
// Те же правила проверяет API, а здесь они нужны, чтобы вместо формы сразу объяснить, почему её нет.
async function Claim({ params }: Pick<PageProps<"/reviewers/[id]/claim">, "params">) {
  const { id } = await params;
  if (!isGuid(id)) notFound();

  const reviewer = await getReviewer(id);
  if (!reviewer) notFound();

  const api = await userApi();
  const [me, claims, suggestions] = await Promise.all([
    api.GET("/api/auth/me"),
    api.GET("/api/claims/my"),
    api.GET("/api/suggestions/my"),
  ]);
  // proxy.ts уже увёл гостей без cookie, здесь отсеиваются просроченные токены.
  if (me.response.status === 401) redirect(`/login?returnTo=${encodeURIComponent(`/reviewers/${id}/claim`)}`);
  const user = me.data ?? failed(me.response);
  const pendingClaim = (claims.data ?? failed(claims.response)).find((claim) => claim.status === "Pending");
  const pendingSelf = (suggestions.data ?? failed(suggestions.response)).find((s) => s.isSelf && s.status === "Pending");

  const profileLink = (reviewerId: string | null, name: ReactNode) =>
    <Link href={`/reviewers/${reviewerId}`} className="text-primary underline-offset-4 hover:underline">{name}</Link>;

  let blocked: ReactNode = null;
  if (reviewer.userId === user.id) blocked = "Это уже ваш профиль.";
  else if (reviewer.userId) blocked = "Этот профиль уже привязан к другому аккаунту.";
  else if (user.reviewerId) blocked = <>Ваш аккаунт уже привязан к {profileLink(user.reviewerId, "другому профилю")}.</>;
  else if (pendingClaim) blocked = <>Ваш запрос на привязку профиля {profileLink(pendingClaim.reviewerId, pendingClaim.reviewerName)} ждёт модератора.</>;
  else if (pendingSelf) blocked = <>Ваша заявка на нового обзорщика «{pendingSelf.fullName}» с отметкой «это я» ждёт модератора.</>;

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-6">
      <div className="flex flex-col gap-2">
        <h1 className="text-3xl leading-tight font-bold">Это вы?</h1>
        <p className="text-muted-foreground">
          Привяжите профиль {profileLink(id, reviewer.fullName)} к своему аккаунту. Модератор проверит, что это
          действительно вы, и после этого профиль будет отмечен как подтверждённый.
        </p>
      </div>
      {blocked ? (
        <>
          <p className="rounded-2xl border bg-card p-5">{blocked}</p>
          <Link href="/me" className={`${buttonVariants({ variant: "outline" })} self-start`}>Мои заявки</Link>
        </>
      ) : (
        <ClaimForm action={claimReviewer.bind(null, id)} />
      )}
    </div>
  );
}
