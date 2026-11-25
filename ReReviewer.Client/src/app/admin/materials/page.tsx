import { ExternalLink } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { Suspense } from "react";
import { moderateMaterialSuggestion } from "@/lib/admin/actions";
import { failed, userApi } from "@/lib/api/client";
import { formatDate, materialFormatLabel, pluralize } from "@/lib/format";
import { DecisionForm } from "../decision-form";
import { NoAccess } from "../no-access";
import { Pager, pageParam } from "../pager";

export const metadata: Metadata = {
  title: "Материалы",
};

const PAGE_SIZE = 20;

export default function MaterialModerationPage({ searchParams }: PageProps<"/admin/materials">) {
  return (
    <Suspense fallback={<p className="text-muted-foreground">Загружаем заявки…</p>}>
      <Queue searchParams={searchParams} />
    </Suspense>
  );
}

// Материалы, которые предложили читатели, — первой идёт заявка, что ждёт дольше всех. Ссылку стоит открыть
// и проверить, что материал действительно этого обзорщика и про эту игру.
async function Queue({ searchParams }: Pick<PageProps<"/admin/materials">, "searchParams">) {
  const page = pageParam((await searchParams).page);

  const api = await userApi();
  const { data, response } = await api.GET("/api/materialsuggestions/moderation", { params: { query: { page, pageSize: PAGE_SIZE } } });
  // proxy.ts уже увёл гостей без cookie, здесь отсеиваются просроченные токены и заблокированные аккаунты.
  if (response.status === 401) redirect("/login?returnTo=/admin/materials");
  if (response.status === 403) return <NoAccess role="модераторам и администраторам" />;
  const queue = data ?? failed(response);

  if (queue.totalCount === 0)
    return <p className="text-muted-foreground">Новых материалов нет.</p>;

  return (
    <div className="flex flex-col gap-4">
      <p className="text-muted-foreground">
        Ждут проверки: {queue.totalCount} {pluralize(queue.totalCount, "материал", "материала", "материалов")}.
        Одобренный материал сразу появится на странице обзорщика, и его можно будет отметить в отзыве.
        Здесь только заявки со страниц обзорщиков: материалы, добавленные в отзыв, проверяются вместе с отзывом.
      </p>
      {queue.items.length === 0 && <p className="text-muted-foreground">На этой странице заявок больше нет.</p>}
      {queue.items.map((suggestion) => (
        <article key={suggestion.id} className="flex flex-col gap-3 rounded-2xl border bg-card p-5">
          <div className="flex flex-col gap-1">
            <h2 className="text-lg font-bold">
              <a href={suggestion.url} target="_blank" rel="noopener noreferrer nofollow" className="inline-flex items-center gap-1.5 hover:underline">
                {suggestion.title}
                <ExternalLink aria-hidden="true" className="size-4 shrink-0 text-muted-foreground" />
              </a>
            </h2>
            {/* Сам адрес тоже ссылка: модератор видит, куда ведёт материал, и открывает его отсюда. */}
            <a
              href={suggestion.url}
              target="_blank"
              rel="noopener noreferrer nofollow"
              className="self-start text-sm break-all text-primary underline underline-offset-4 hover:text-primary/80"
            >
              {suggestion.url}
            </a>
          </div>
          <p>
            <Link href={`/reviewers/${suggestion.reviewerId}`} className="font-semibold hover:underline">{suggestion.reviewerName}</Link>
            {" · "}
            {[
              materialFormatLabel(suggestion.format),
              suggestion.gameTitle,
              suggestion.publishedAt && formatDate(suggestion.publishedAt),
            ].filter(Boolean).join(" · ")}
          </p>
          <p className="text-sm text-muted-foreground">
            {formatDate(suggestion.createdAt)} · предложил {suggestion.userName}
          </p>
          <DecisionForm action={moderateMaterialSuggestion.bind(null, suggestion.id ?? "", suggestion.reviewerId)} />
        </article>
      ))}
      <Pager basePath="/admin/materials" page={queue.page} totalPages={queue.totalPages} />
    </div>
  );
}
