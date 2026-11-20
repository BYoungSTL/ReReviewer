import type { Metadata } from "next";
import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { Suspense } from "react";
import { ReviewCard } from "@/components/review-card";
import { ReviewForm } from "@/components/review-form";
import { buttonVariants } from "@/components/ui/button";
import { failed, userApi } from "@/lib/api/client";
import { getCriteria, getReviewer, getReviewerMaterials, isGuid } from "@/lib/api/public";
import { getCurrentUser } from "@/lib/auth/session";
import { createReview } from "@/lib/reviews/actions";
import { reviewCriteria } from "@/lib/reviews/criteria";

export const metadata: Metadata = {
  title: "Новый отзыв",
  robots: { index: false },
};

export default function NewReviewPage({ params }: PageProps<"/reviewers/[id]/review">) {
  return (
    <Suspense fallback={<p className="text-muted-foreground">Загружаем форму…</p>}>
      <NewReview params={params} />
    </Suspense>
  );
}

async function NewReview({ params }: Pick<PageProps<"/reviewers/[id]/review">, "params">) {
  const { id } = await params;
  if (!isGuid(id)) notFound();

  const [reviewer, materials, criteria] = await Promise.all([
    getReviewer(id),
    getReviewerMaterials(id),
    getCriteria(),
  ]);
  if (!reviewer) notFound();

  const api = await userApi();
  const { data: myReviews, response } = await api.GET("/api/reviews/my");
  // proxy.ts уже увёл гостей без cookie, здесь отсеиваются просроченные токены.
  if (response.status === 401) redirect(`/login?returnTo=${encodeURIComponent(`/reviewers/${id}/review`)}`);
  const existing = (myReviews ?? failed(response)).find((review) => review.reviewerId === id);
  const user = await getCurrentUser();

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-3xl font-semibold tracking-tight">Отзыв об обзорщике</h1>
        <p className="text-muted-foreground">
          <Link href={`/reviewers/${id}`} className="underline underline-offset-4">{reviewer.fullName}</Link>
        </p>
      </div>

      {existing ? (
        <>
          <p>Вы уже оставили отзыв об этом обзорщике. Написать можно только один, но этот можно изменить.</p>
          <ReviewCard review={existing} own />
          <Link href="/me" className={`${buttonVariants({ variant: "outline" })} self-start`}>
            Все мои отзывы
          </Link>
        </>
      ) : (
        // Next.js хранит покинутую страницу скрытой вместе с полями формы (Activity), в том числе после выхода.
        // Без key черновик одного пользователя увидел бы и мог отправить следующий, вошедший в этом же браузере.
        <ReviewForm
          key={user?.id}
          action={createReview.bind(null, id)}
          materials={materials}
          criteria={reviewCriteria(criteria, reviewer)}
          submitLabel="Отправить отзыв"
          pendingLabel="Отправляем…"
          hint="Отзыв появится на странице обзорщика после модерации."
        />
      )}
    </div>
  );
}
