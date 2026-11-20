import { Award } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { Suspense } from "react";
import { moderateNomination } from "@/lib/admin/actions";
import { failed, userApi } from "@/lib/api/client";
import { formatDate, pluralize } from "@/lib/format";
import { DecisionForm } from "../decision-form";
import { NoAccess } from "../no-access";
import { Pager, pageParam } from "../pager";

export const metadata: Metadata = {
  title: "Знаки",
};

const PAGE_SIZE = 20;

export default function BadgeModerationPage({ searchParams }: PageProps<"/admin/badges">) {
  return (
    <Suspense fallback={<p className="text-muted-foreground">Загружаем номинации…</p>}>
      <Queue searchParams={searchParams} />
    </Suspense>
  );
}

// Номинации обзорщиков на знаки, сгруппированные по паре «обзорщик + знак»: первой идёт та, за которую больше всего
// номинаций, при равенстве — та, что ждёт дольше. «Выдать» вручает знак, «Отклонить» снимает номинации пары.
async function Queue({ searchParams }: Pick<PageProps<"/admin/badges">, "searchParams">) {
  const page = pageParam((await searchParams).page);

  const api = await userApi();
  const { data, response } = await api.GET("/api/nominations/moderation", { params: { query: { page, pageSize: PAGE_SIZE } } });
  // proxy.ts уже увёл гостей без cookie, здесь отсеиваются просроченные токены и заблокированные аккаунты.
  if (response.status === 401) redirect("/login?returnTo=/admin/badges");
  if (response.status === 403) return <NoAccess role="модераторам и администраторам" />;
  const queue = data ?? failed(response);

  if (queue.totalCount === 0)
    return <p className="text-muted-foreground">Номинаций на знаки нет.</p>;

  return (
    <div className="flex flex-col gap-4">
      <p className="text-muted-foreground">
        Ждут решения: {queue.totalCount} {pluralize(queue.totalCount, "знак", "знака", "знаков")} у обзорщиков.
        Первыми идут те, за которые больше номинаций. Выданный знак появится в профиле обзорщика, а номинации на него закроются;
        после отклонения номинировать можно снова.
      </p>
      {queue.items.length === 0 && <p className="text-muted-foreground">На этой странице номинаций больше нет.</p>}
      {queue.items.map((item) => (
        <article key={`${item.reviewerId}:${item.badgeId}`} className="flex flex-col gap-3 rounded-2xl border bg-card p-5">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div className="flex flex-col gap-1">
              <h2 className="text-lg font-bold">
                <Link href={`/reviewers/${item.reviewerId}`} className="hover:underline">{item.reviewerName}</Link>
              </h2>
              <p className="inline-flex items-center gap-1.5 font-semibold text-score-mid">
                <Award aria-hidden="true" className="size-4" />
                {item.badgeTitle}
              </p>
            </div>
            <p className="shrink-0 text-right">
              <span className="font-display text-3xl font-bold tabular-nums">{item.nominationCount}</span>
              <span className="block text-sm text-muted-foreground">
                {pluralize(item.nominationCount, "номинация", "номинации", "номинаций")}
              </span>
            </p>
          </div>
          <p className="text-sm text-muted-foreground">
            {item.nominationCount === 1
              ? <>Номинирован {formatDate(item.firstNominatedAt)}</>
              : <>Первая номинация {formatDate(item.firstNominatedAt)}, последняя {formatDate(item.lastNominatedAt)}</>}
          </p>
          <DecisionForm
            action={moderateNomination.bind(null, item.reviewerId, item.badgeId)}
            approveLabel="Выдать знак"
            rejectLabel="Отклонить"
          />
        </article>
      ))}
      <Pager basePath="/admin/badges" page={queue.page} totalPages={queue.totalPages} />
    </div>
  );
}
