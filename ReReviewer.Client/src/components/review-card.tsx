import Link from "next/link";
import type { ReactNode } from "react";
import { Avatar } from "@/components/avatar";
import { ScoreBadge } from "@/components/score-badge";
import { buttonVariants } from "@/components/ui/button";
import type { ReviewViewModel } from "@/lib/api/schema";
import { formatDate, reviewStatusLabel } from "@/lib/format";
import { SCORE_TEXT, scoreTone } from "@/lib/score";

// Отзыв пользователя об обзорщике. На странице обзорщика ссылка на него не нужна (showReviewer = false),
// в «Моих отзывах» — нужна. У своего отзыва (own) видны статус модерации и ссылка на правку.
// meta дописывается в строку с датой, children — под отзывом (например, кнопки модерации).
export function ReviewCard({ review, showReviewer = false, own = false, meta, children }: {
  review: ReviewViewModel;
  showReviewer?: boolean;
  own?: boolean;
  meta?: ReactNode;
  children?: ReactNode;
}) {
  return (
    <article className="flex flex-col gap-3 rounded-2xl border bg-card p-5">
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          {showReviewer
            ? <Avatar id={review.reviewerId} name={review.reviewerName ?? ""} size="sm" />
            : <Avatar id={review.userId} name={review.userName ?? ""} size="sm" />}
          <div className="min-w-0">
            <div className="font-bold">
              {showReviewer ? (
                <Link href={`/reviewers/${review.reviewerId}`} className="hover:underline">
                  {review.reviewerName}
                </Link>
              ) : review.userName}
            </div>
            <div className="text-sm text-muted-foreground">
              {formatDate(review.createdAt)}
              {own && <> · {reviewStatusLabel(review.status)}</>}
              {meta && <> · {meta}</>}
            </div>
          </div>
        </div>
        <ScoreBadge score={review.score} />
      </div>

      <p className="whitespace-pre-line text-foreground/90">{review.text}</p>

      {review.criterionScores.length > 0 && (
        <ul className="flex flex-wrap gap-2 text-sm">
          {review.criterionScores.map((score) => (
            <li key={score.criterionId} className="rounded-full bg-secondary px-2.5 py-0.5 text-secondary-foreground/80">
              {score.criterionTitle} <b className={SCORE_TEXT[scoreTone(score.score)]}>{score.score}</b>
            </li>
          ))}
        </ul>
      )}

      {review.materials.length > 0 && (
        <p className="text-sm text-muted-foreground">
          {review.materials.length === 1 ? "По материалу: " : "По материалам: "}
          {review.materials.map((material, index) => (
            <span key={material.id}>
              {index > 0 && ", "}
              {material.url ? (
                <a href={material.url} target="_blank" rel="noopener noreferrer nofollow" className="text-primary underline-offset-4 hover:underline">
                  {material.title}
                </a>
              ) : material.title}
            </span>
          ))}
        </p>
      )}

      {own && (
        <Link href={`/reviews/${review.id}/edit`} className={`${buttonVariants({ variant: "outline", size: "sm" })} self-start`}>
          Изменить
        </Link>
      )}
      {children}
    </article>
  );
}
