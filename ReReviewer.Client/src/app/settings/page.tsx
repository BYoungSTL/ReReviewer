import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { Suspense, type ReactNode } from "react";
import { failed, userApi } from "@/lib/api/client";
import { getReviewers } from "@/lib/api/public";
import { avatarUrl } from "@/lib/avatar";
import { AvatarSettings } from "./avatar-settings";
import { ClaimSelectForm } from "./claim-select-form";
import { PasswordForm } from "./password-form";
import { ProfileForm } from "./profile-form";

export const metadata: Metadata = {
  title: "Настройки",
  robots: { index: false },
};

export default function SettingsPage() {
  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-6">
      <h1 className="text-3xl leading-tight font-bold">Настройки</h1>
      <Suspense fallback={<p className="text-muted-foreground">Загружаем профиль…</p>}>
        <Settings />
      </Suspense>
    </div>
  );
}

async function Settings() {
  const api = await userApi();
  const [me, claims, suggestions, reviewers] = await Promise.all([
    api.GET("/api/auth/me"),
    api.GET("/api/claims/my"),
    api.GET("/api/suggestions/my"),
    getReviewers(),
  ]);
  // proxy.ts уже увёл гостей без cookie, здесь отсеиваются просроченные токены.
  if (me.response.status === 401) redirect("/login?returnTo=/settings");
  const user = me.data ?? failed(me.response);
  const userId = user.id ?? "";
  const src = avatarUrl(userId, user.avatarUpdatedAt);

  return (
    <>
      <Section title="Аватарка">
        <AvatarSettings key={src ?? "none"} userId={userId} name={user.name} src={src} />
      </Section>
      <Section title="Профиль">
        <ProfileForm name={user.name} email={user.email} />
      </Section>
      <Section title="Пароль">
        <PasswordForm />
      </Section>
      <Section title="Профиль обзорщика" description="Если вы журналист или блогер, привяжите свой профиль к аккаунту: модератор проверит, что это вы, и профиль будет отмечен как подтверждённый.">
        <ReviewerLink
          userReviewerId={user.reviewerId}
          reviewers={reviewers}
          pendingClaim={(claims.data ?? failed(claims.response)).find((claim) => claim.status === "Pending")}
          pendingSelf={(suggestions.data ?? failed(suggestions.response)).find((s) => s.isSelf && s.status === "Pending")}
        />
      </Section>
    </>
  );
}

// Привязать можно только свободный профиль и только если у аккаунта ещё нет привязки или заявки на неё.
function ReviewerLink({ userReviewerId, reviewers, pendingClaim, pendingSelf }: {
  userReviewerId: string | null;
  reviewers: Awaited<ReturnType<typeof getReviewers>>;
  pendingClaim?: { reviewerId: string; reviewerName: string | null };
  pendingSelf?: { fullName: string };
}) {
  const link = (id: string | null, name: ReactNode) =>
    <Link href={`/reviewers/${id}`} className="text-primary underline-offset-4 hover:underline">{name}</Link>;

  if (userReviewerId) {
    const linked = reviewers.find((reviewer) => reviewer.id === userReviewerId);

    return <p>Ваш аккаунт привязан к профилю {link(userReviewerId, linked?.fullName ?? "обзорщика")}.</p>;
  }
  if (pendingClaim)
    return <p>Запрос на привязку профиля {link(pendingClaim.reviewerId, pendingClaim.reviewerName)} ждёт модератора.</p>;
  if (pendingSelf)
    return <p>Заявка на нового обзорщика «{pendingSelf.fullName}» с отметкой «это я» ждёт модератора.</p>;

  const free = reviewers
    .filter((reviewer) => reviewer.userId === null && reviewer.id !== null)
    .map((reviewer) => ({ id: reviewer.id!, name: reviewer.fullName }))
    .sort((a, b) => a.name.localeCompare(b.name, "ru"));

  return (
    <div className="flex flex-col gap-4">
      {free.length > 0 && <ClaimSelectForm reviewers={free} />}
      <p className="text-sm text-muted-foreground">
        Вашего профиля нет на сайте?{" "}
        <Link href="/reviewers/new?self=1" className="text-primary underline-offset-4 hover:underline">Предложите себя как нового обзорщика</Link>.
      </p>
    </div>
  );
}

function Section({ title, description, children }: { title: string; description?: string; children: ReactNode }) {
  return (
    <section className="flex flex-col gap-4 rounded-3xl border bg-card p-6">
      <div className="flex flex-col gap-1">
        <h2 className="font-display text-xl font-bold">{title}</h2>
        {description && <p className="text-sm text-muted-foreground">{description}</p>}
      </div>
      {children}
    </section>
  );
}
