import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { Suspense } from "react";
import { moderateClaim } from "@/lib/admin/actions";
import { failed, userApi } from "@/lib/api/client";
import { formatDate, pluralize } from "@/lib/format";
import { DecisionForm } from "../decision-form";
import { NoAccess } from "../no-access";
import { Pager, pageParam } from "../pager";

export const metadata: Metadata = {
  title: "Привязка профилей",
};

const PAGE_SIZE = 20;

export default function ClaimModerationPage({ searchParams }: PageProps<"/admin/claims">) {
  return (
    <Suspense fallback={<p className="text-muted-foreground">Загружаем запросы…</p>}>
      <Queue searchParams={searchParams} />
    </Suspense>
  );
}

// Запросы «это я», первым — тот, что ждёт дольше всех.
async function Queue({ searchParams }: Pick<PageProps<"/admin/claims">, "searchParams">) {
  const page = pageParam((await searchParams).page);

  const api = await userApi();
  const { data, response } = await api.GET("/api/claims/moderation", { params: { query: { page, pageSize: PAGE_SIZE } } });
  // proxy.ts уже увёл гостей без cookie, здесь отсеиваются просроченные токены и заблокированные аккаунты.
  if (response.status === 401) redirect("/login?returnTo=/admin/claims");
  if (response.status === 403) return <NoAccess role="модераторам и администраторам" />;
  const queue = data ?? failed(response);

  if (queue.totalCount === 0)
    return <p className="text-muted-foreground">Запросов на привязку нет.</p>;

  return (
    <div className="flex flex-col gap-4">
      <p className="text-muted-foreground">
        Ждут проверки: {queue.totalCount} {pluralize(queue.totalCount, "запрос", "запроса", "запросов")}.
        Одобрение привязывает профиль к аккаунту автора запроса, остальные запросы на этот профиль отклоняются.
      </p>
      {queue.items.length === 0 && <p className="text-muted-foreground">На этой странице запросов больше нет.</p>}
      {queue.items.map((claim) => (
        <article key={claim.id} className="flex flex-col gap-3 rounded-2xl border bg-card p-5">
          <div className="flex flex-col gap-1">
            <h2 className="text-lg font-bold">
              <Link href={`/reviewers/${claim.reviewerId}`} className="hover:underline">{claim.reviewerName}</Link>
            </h2>
            <p className="text-sm text-muted-foreground">
              {formatDate(claim.createdAt)} · запрос от пользователя {claim.userName}
            </p>
          </div>
          <div className="flex flex-col gap-1">
            <span className="text-sm font-semibold text-secondary-foreground/80">Как проверить</span>
            <p className="break-words whitespace-pre-line text-foreground/90">{claim.note}</p>
          </div>
          <DecisionForm action={moderateClaim.bind(null, claim.id ?? "", claim.reviewerId)} />
        </article>
      ))}
      <Pager basePath="/admin/claims" page={queue.page} totalPages={queue.totalPages} />
    </div>
  );
}
