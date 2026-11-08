import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { Suspense } from "react";
import { ReviewCard } from "@/components/review-card";
import { buttonVariants } from "@/components/ui/button";
import { moderateReview } from "@/lib/admin/actions";
import { failed, userApi } from "@/lib/api/client";
import { formatDate, pluralize } from "@/lib/format";
import { NoAccess } from "../no-access";
import { ReviewModeration } from "./review-moderation";

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
  const { page: pageParam } = await searchParams;
  const page = Math.max(1, Math.trunc(Number(pageParam)) || 1);

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
          <ReviewModeration action={moderateReview.bind(null, review.id ?? "", review.reviewerId)} />
        </ReviewCard>
      ))}
      <Pager page={queue.page} totalPages={queue.totalPages} />
    </div>
  );
}

function Pager({ page, totalPages }: { page: number; totalPages: number }) {
  if (totalPages <= 1 && page <= 1) return null;

  const link = buttonVariants({ variant: "outline", size: "sm" });

  return (
    <nav className="flex items-center gap-3 self-center">
      {page > 1 && <Link href={`/admin/reviews?page=${Math.min(page - 1, Math.max(totalPages, 1))}`} className={link}>Назад</Link>}
      <span className="text-sm text-muted-foreground">Страница {page} из {Math.max(totalPages, 1)}</span>
      {page < totalPages && <Link href={`/admin/reviews?page=${page + 1}`} className={link}>Дальше</Link>}
    </nav>
  );
}
