import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { Suspense } from "react";
import { ReviewCard } from "@/components/review-card";
import { moderateReview } from "@/lib/admin/actions";
import { failed, userApi } from "@/lib/api/client";
import { formatDate, pluralize } from "@/lib/format";
import { DecisionForm } from "../decision-form";
import { NoAccess } from "../no-access";
import { Pager, pageParam } from "../pager";

export const metadata: Metadata = {
  title: "Отзывы",
};

const PAGE_SIZE = 20;

export default function ReviewModerationPage({ searchParams }: PageProps<"/admin/reviews">) {
  return (
    <Suspense fallback={<p className="text-muted-foreground">Загружаем очередь…</p>}>
      <Queue searchParams={searchParams} />
    </Suspense>
  );
}

// Очередь — отзывы со статусом «На модерации», первым идёт тот, что ждёт дольше всех (с создания или последней правки).
async function Queue({ searchParams }: Pick<PageProps<"/admin/reviews">, "searchParams">) {
  const page = pageParam((await searchParams).page);

  const api = await userApi();
  const { data, response } = await api.GET("/api/reviews/moderation", { params: { query: { page, pageSize: PAGE_SIZE } } });
  // proxy.ts уже увёл гостей без cookie, здесь отсеиваются просроченные токены и заблокированные аккаунты.
  if (response.status === 401) redirect("/login?returnTo=/admin/reviews");
  if (response.status === 403) return <NoAccess role="модераторам и администраторам" />;
  const queue = data ?? failed(response);

  if (queue.totalCount === 0)
    return <p className="text-muted-foreground">Очередь пуста: все отзывы проверены.</p>;

  return (
    <div className="flex flex-col gap-4">
      <p className="text-muted-foreground">
        Ждут проверки: {queue.totalCount} {pluralize(queue.totalCount, "отзыв", "отзыва", "отзывов")}.
        Первым идёт тот, что ждёт дольше всех. Изменённый автором отзыв снова попадает в очередь.
      </p>
      {queue.items.length === 0 && <p className="text-muted-foreground">На этой странице отзывов больше нет.</p>}
      {queue.items.map((review) => (
        <ReviewCard
          key={review.id}
          review={review}
          showReviewer
          meta={<>
            автор {review.userName}
            {review.updatedAt && <> · изменён {formatDate(review.updatedAt)}</>}
          </>}
        >
          <DecisionForm action={moderateReview.bind(null, review.id ?? "", review.reviewerId)} />
        </ReviewCard>
      ))}
      <Pager basePath="/admin/reviews" page={queue.page} totalPages={queue.totalPages} />
    </div>
  );
}
