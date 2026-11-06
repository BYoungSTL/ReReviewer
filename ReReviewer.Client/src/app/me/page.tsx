import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { Suspense } from "react";
import { ReviewCard } from "@/components/review-card";
import { failed, userApi } from "@/lib/api/client";
import { getCurrentUser } from "@/lib/auth/session";

export const metadata: Metadata = {
  title: "Мои отзывы",
  robots: { index: false },
};

export default function MyReviewsPage() {
  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-3xl font-semibold tracking-tight">Мои отзывы</h1>
      <Suspense fallback={<p className="text-muted-foreground">Загружаем отзывы…</p>}>
        <MyReviews />
      </Suspense>
    </div>
  );
}

async function MyReviews() {
  // proxy.ts уже увёл гостей без cookie, здесь отсеиваются просроченные токены.
  if (!(await getCurrentUser())) redirect("/login?returnTo=/me");

  const api = await userApi();
  const { data, response } = await api.GET("/api/reviews/my");
  if (response.status === 401) redirect("/login?returnTo=/me");
  const reviews = data ?? failed(response);

  if (reviews.length === 0)
    return (
      <p className="text-muted-foreground">
        Вы ещё не написали ни одного отзыва. Выберите обзорщика на <Link href="/" className="underline underline-offset-4">главной</Link>.
      </p>
    );

  return (
    <div className="flex flex-col gap-4">
      {reviews.map((review) => (
        <ReviewCard key={review.id} review={review} showReviewer showStatus />
      ))}
    </div>
  );
}
