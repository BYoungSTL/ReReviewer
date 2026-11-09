import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { Suspense } from "react";
import { moderateSuggestion } from "@/lib/admin/actions";
import { failed, userApi } from "@/lib/api/client";
import { formatDate, pluralize } from "@/lib/format";
import { DecisionForm } from "../decision-form";
import { NoAccess } from "../no-access";
import { Pager, pageParam } from "../pager";

export const metadata: Metadata = {
  title: "Новые обзорщики",
};

const PAGE_SIZE = 20;

export default function SuggestionModerationPage({ searchParams }: PageProps<"/admin/reviewers">) {
  return (
    <Suspense fallback={<p className="text-muted-foreground">Загружаем заявки…</p>}>
      <Queue searchParams={searchParams} />
    </Suspense>
  );
}

// Заявки на новых обзорщиков, первой — та, что ждёт дольше всех.
async function Queue({ searchParams }: Pick<PageProps<"/admin/reviewers">, "searchParams">) {
  const page = pageParam((await searchParams).page);

  const api = await userApi();
  const { data, response } = await api.GET("/api/suggestions/moderation", { params: { query: { page, pageSize: PAGE_SIZE } } });
  // proxy.ts уже увёл гостей без cookie, здесь отсеиваются просроченные токены и заблокированные аккаунты.
  if (response.status === 401) redirect("/login?returnTo=/admin/reviewers");
  if (response.status === 403) return <NoAccess role="модераторам и администраторам" />;
  const queue = data ?? failed(response);

  if (queue.totalCount === 0)
    return <p className="text-muted-foreground">Новых заявок нет.</p>;

  return (
    <div className="flex flex-col gap-4">
      <p className="text-muted-foreground">
        Ждут проверки: {queue.totalCount} {pluralize(queue.totalCount, "заявка", "заявки", "заявок")}.
        После одобрения обзорщик сразу появится на сайте.
      </p>
      {queue.items.length === 0 && <p className="text-muted-foreground">На этой странице заявок больше нет.</p>}
      {queue.items.map((suggestion) => (
        <article key={suggestion.id} className="flex flex-col gap-3 rounded-2xl border bg-card p-5">
          <div className="flex flex-col gap-1">
            <h2 className="text-lg font-bold">{suggestion.fullName}</h2>
            <p className="text-sm text-muted-foreground">
              {formatDate(suggestion.createdAt)} · предложил {suggestion.userName}
            </p>
          </div>
          {suggestion.isSelf && (
            <p className="self-start rounded-full bg-primary/15 px-3 py-1 text-sm font-semibold text-primary">
              Предлагает себя: после одобрения профиль привяжется к его аккаунту
            </p>
          )}
          {suggestion.genres.length > 0 && (
            <ul className="flex flex-wrap gap-1.5">
              {suggestion.genres.map((genre) => (
                <li key={genre.id} className="rounded-full bg-secondary px-2.5 py-0.5 text-sm text-secondary-foreground/80">{genre.title}</li>
              ))}
            </ul>
          )}
          {suggestion.note
            ? <p className="break-words whitespace-pre-line text-foreground/90">{suggestion.note}</p>
            : <p className="text-sm text-muted-foreground">Автор не написал, где обзорщик публикуется.</p>}
          <DecisionForm action={moderateSuggestion.bind(null, suggestion.id ?? "")} />
        </article>
      ))}
      <Pager basePath="/admin/reviewers" page={queue.page} totalPages={queue.totalPages} />
    </div>
  );
}
