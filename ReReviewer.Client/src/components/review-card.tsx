import Link from "next/link";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import type { ReviewViewModel } from "@/lib/api/schema";
import { formatDate, reviewStatusLabel } from "@/lib/format";

// Отзыв пользователя об обзорщике. На странице обзорщика ссылка на него не нужна (showReviewer = false),
// в «Моих отзывах» — нужна, там же показывается статус модерации.
export function ReviewCard({ review, showReviewer = false, showStatus = false }: {
  review: ReviewViewModel;
  showReviewer?: boolean;
  showStatus?: boolean;
}) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-baseline justify-between gap-4">
          <span>
            {showReviewer ? (
              <Link href={`/reviewers/${review.reviewerId}`} className="hover:underline">
                {review.reviewerName}
              </Link>
            ) : review.userName}
          </span>
          <span className="shrink-0 tabular-nums">{review.score}/10</span>
        </CardTitle>
        <CardDescription>
          {formatDate(review.createdAt)}
          {showStatus && <> · {reviewStatusLabel(review.status)}</>}
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <p className="whitespace-pre-line">{review.text}</p>
        {review.criterionScores.length > 0 && (
          <ul className="flex flex-wrap gap-x-4 gap-y-1 text-muted-foreground">
            {review.criterionScores.map((score) => (
              <li key={score.criterionId}>
                {score.criterionTitle}: <span className="text-foreground tabular-nums">{score.score}</span>
              </li>
            ))}
          </ul>
        )}
        {review.materials.length > 0 && (
          <div className="text-muted-foreground">
            По материалам:{" "}
            {review.materials.map((material, index) => (
              <span key={material.id}>
                {index > 0 && ", "}
                {material.url ? (
                  <a href={material.url} target="_blank" rel="noopener noreferrer nofollow" className="underline underline-offset-4">
                    {material.title}
                  </a>
                ) : material.title}
              </span>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
