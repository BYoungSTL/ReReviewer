"use client";

import { useInfiniteQuery } from "@tanstack/react-query";
import { ReviewCard } from "@/components/review-card";
import { Button } from "@/components/ui/button";
import { browserApi } from "@/lib/api/browser";
import type { PagedViewModelOfReviewViewModel } from "@/lib/api/schema";

// Чьи опубликованные отзывы: об обзорщике (на его странице) или написанные пользователем (на странице пользователя).
// У отзывов пользователя в карточке показывается обзорщик, о котором отзыв, у отзывов об обзорщике — автор.
export type ReviewsSource = { kind: "reviewer" | "user"; id: string };

// Первая страница приходит с сервера (её видят поисковики), следующие подгружаются в браузере
// через BFF-прокси по кнопке «Показать ещё».
export function ReviewsList({ source, firstPage }: {
  source: ReviewsSource;
  firstPage: PagedViewModelOfReviewViewModel;
}) {
  const { data, fetchNextPage, hasNextPage, isFetchingNextPage, isError } = useInfiniteQuery({
    // Первая страница входит в ключ, потому что initialData применяется только к новой записи кэша. Иначе при возврате
    // на страницу свежие данные с сервера игнорировались бы, и, например, снятый после правки отзыв оставался бы в списке.
    queryKey: [source.kind, source.id, "reviews", firstPage],
    queryFn: async ({ pageParam }) => {
      const params = { params: { path: { id: source.id }, query: { page: pageParam, pageSize: firstPage.pageSize } } };
      const { data, response } = source.kind === "reviewer"
        ? await browserApi.GET("/api/reviewers/{id}/reviews", params)
        : await browserApi.GET("/api/users/{id}/reviews", params);
      if (!data) throw new Error(`Failed to load reviews: ${response.status}`);

      return data;
    },
    initialPageParam: 1,
    initialData: { pages: [firstPage], pageParams: [1] },
    getNextPageParam: (last) => (last.page < last.totalPages ? last.page + 1 : undefined),
  });

  const reviews = data.pages.flatMap((page) => page.items);
  if (reviews.length === 0)
    return <p className="text-muted-foreground">Опубликованных отзывов пока нет.</p>;

  return (
    <div className="flex flex-col gap-4">
      {reviews.map((review) => (
        <ReviewCard key={review.id} review={review} showReviewer={source.kind === "user"} />
      ))}
      {isError && <p className="text-sm text-destructive">Не удалось загрузить отзывы. Попробуйте ещё раз.</p>}
      {hasNextPage && (
        <Button variant="outline" className="self-center" disabled={isFetchingNextPage} onClick={() => fetchNextPage()}>
          {isFetchingNextPage ? "Загружаем…" : "Показать ещё"}
        </Button>
      )}
    </div>
  );
}
