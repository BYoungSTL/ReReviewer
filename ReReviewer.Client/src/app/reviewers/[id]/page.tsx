import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { Suspense } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { getReviewer, getReviewerRating, getReviewerReviews, isGuid } from "@/lib/api/public";
import { formatScore, pluralize } from "@/lib/format";
import { ReviewsList } from "./reviews-list";

// Главная страница для поиска: «отзывы о <обзорщик>». Заголовок и описание — в метатегах и Open Graph,
// чтобы ссылка в Telegram или Discord разворачивалась в карточку. Данные кэшируются (lib/api/public.ts),
// поэтому generateMetadata и страница не запрашивают API дважды.
export async function generateMetadata({ params }: PageProps<"/reviewers/[id]">): Promise<Metadata> {
  const { id } = await params;
  const reviewer = isGuid(id) ? await getReviewer(id) : null;
  if (!reviewer) return { title: "Обзорщик не найден" };

  const rating = await getReviewerRating(id);
  const description = rating?.reviewCount && rating.averageScore !== null
    ? `${reviewer.fullName}: средняя оценка ${formatScore(rating.averageScore)} из 10 по ${rating.reviewCount} ${pluralize(rating.reviewCount, "отзыву", "отзывам", "отзывам")} на ReReviewer.`
    : `Отзывы о материалах ${reviewer.fullName} на ReReviewer.`;

  return {
    title: reviewer.fullName,
    description,
    openGraph: { title: reviewer.fullName, description, type: "profile" },
  };
}

export default function ReviewerPage({ params }: PageProps<"/reviewers/[id]">) {
  return (
    <Suspense fallback={<p className="text-muted-foreground">Загружаем профиль…</p>}>
      <ReviewerProfile params={params} />
    </Suspense>
  );
}

async function ReviewerProfile({ params }: Pick<PageProps<"/reviewers/[id]">, "params">) {
  const { id } = await params;
  if (!isGuid(id)) notFound();

  const [reviewer, rating, reviews] = await Promise.all([
    getReviewer(id),
    getReviewerRating(id),
    getReviewerReviews(id),
  ]);
  if (!reviewer) notFound();

  return (
    <div className="flex flex-col gap-8">
      <section className="flex flex-col gap-2">
        <h1 className="text-3xl font-semibold tracking-tight">{reviewer.fullName}</h1>
        {reviewer.outlets.length > 0 && (
          <p className="text-muted-foreground">
            {reviewer.outlets.map((outlet) => outlet.outletName).join(", ")}
          </p>
        )}
        {reviewer.genres.length > 0 && (
          <p className="text-sm text-muted-foreground">
            Жанры: {reviewer.genres.map((genre) => genre.title).join(", ")}
          </p>
        )}
        {reviewer.badges.length > 0 && (
          <ul className="flex flex-wrap gap-2 pt-1">
            {reviewer.badges.map((badge) => (
              <li key={badge.badgeId} className="rounded-full bg-muted px-2.5 py-0.5 text-xs">
                {badge.badgeTitle}
              </li>
            ))}
          </ul>
        )}
      </section>

      {rating && rating.reviewCount > 0 && rating.averageScore !== null && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-baseline gap-2">
              <span className="text-3xl tabular-nums">{formatScore(rating.averageScore)}</span>
              <span className="text-sm font-normal text-muted-foreground">
                из 10 · {rating.reviewCount} {pluralize(rating.reviewCount, "отзыв", "отзыва", "отзывов")}
              </span>
            </CardTitle>
          </CardHeader>
          {rating.criteria.length > 0 && (
            <CardContent>
              <dl className="grid gap-x-8 gap-y-1 sm:grid-cols-2">
                {rating.criteria.map((criterion) => (
                  <div key={criterion.criterionId} className="flex justify-between gap-4">
                    <dt className="text-muted-foreground">{criterion.title}</dt>
                    <dd className="tabular-nums">{formatScore(criterion.averageScore)}</dd>
                  </div>
                ))}
              </dl>
            </CardContent>
          )}
        </Card>
      )}

      <section className="flex flex-col gap-4">
        <h2 className="text-xl font-semibold">Отзывы</h2>
        <ReviewsList reviewerId={id} firstPage={reviews} />
      </section>
    </div>
  );
}
