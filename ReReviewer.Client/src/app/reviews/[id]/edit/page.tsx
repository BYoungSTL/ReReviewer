import type { Metadata } from "next";
import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { Suspense } from "react";
import { ReviewForm } from "@/components/review-form";
import { failed, userApi } from "@/lib/api/client";
import { getCriteria, getReviewer, getReviewerMaterials } from "@/lib/api/public";
import type { ReviewViewModel } from "@/lib/api/schema";
import { updateReview, type ReviewFormValues } from "@/lib/reviews/actions";
import { reviewCriteria } from "@/lib/reviews/criteria";

export const metadata: Metadata = {
  title: "Изменить отзыв",
  robots: { index: false },
};

export default function EditReviewPage({ params }: PageProps<"/reviews/[id]/edit">) {
  return (
    <Suspense fallback={<p className="text-muted-foreground">Загружаем отзыв…</p>}>
      <EditReview params={params} />
    </Suspense>
  );
}

async function EditReview({ params }: Pick<PageProps<"/reviews/[id]/edit">, "params">) {
  const { id } = await params;

  const api = await userApi();
  const { data: myReviews, response } = await api.GET("/api/reviews/my");
  // proxy.ts уже увёл гостей без cookie, здесь отсеиваются просроченные токены.
  if (response.status === 401) redirect(`/login?returnTo=${encodeURIComponent(`/reviews/${id}/edit`)}`);
  // Отзыв ищется среди своих: GET /api/reviews/{id} отдал бы и чужой опубликованный.
  const review = (myReviews ?? failed(response)).find((review) => review.id === id);
  if (!review) notFound();

  const [reviewer, materials, criteria] = await Promise.all([
    getReviewer(review.reviewerId),
    getReviewerMaterials(review.reviewerId),
    getCriteria(),
  ]);
  if (!reviewer) notFound();

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-3xl font-semibold tracking-tight">Изменить отзыв</h1>
        <p className="text-muted-foreground">
          <Link href={`/reviewers/${review.reviewerId}`} className="underline underline-offset-4">{reviewer.fullName}</Link>
        </p>
      </div>

      {/* Next.js хранит покинутую страницу скрытой вместе с полями формы (Activity). Черновик при этом сохраняется,
          но когда изменился сам отзыв, форму нужно создать заново: иначе после сохранения в ней останутся
          старые значения, и следующее сохранение затрёт правку. */}
      <ReviewForm
        key={review.updatedAt ?? review.createdAt}
        action={updateReview.bind(null, id)}
        materials={materials}
        criteria={reviewCriteria(criteria, reviewer, review.criterionScores)}
        initialValues={formValues(review)}
        submitLabel="Сохранить изменения"
        pendingLabel="Сохраняем…"
        hint={review.status === "Published"
          ? "После сохранения отзыв снова уйдёт на модерацию и пропадёт со страницы обзорщика, пока не пройдёт проверку."
          : "Изменённый отзыв проходит модерацию заново."}
      />
    </div>
  );
}

const formValues = (review: ReviewViewModel): ReviewFormValues =>
  ({
    text: review.text,
    score: review.score,
    materialIds: review.materials.map((material) => material.id),
    criterionScores: Object.fromEntries(review.criterionScores.map((score) => [score.criterionId, String(score.score)])),
  });
