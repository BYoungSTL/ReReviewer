import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { Suspense } from "react";
import { ReviewCard } from "@/components/review-card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { failed, userApi } from "@/lib/api/client";

export const metadata: Metadata = {
  title: "Мои отзывы",
  robots: { index: false },
};

export default function MyReviewsPage({ searchParams }: PageProps<"/me">) {
  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-3xl font-semibold tracking-tight">Мои отзывы</h1>
      <Suspense>
        <SentNotice searchParams={searchParams} />
      </Suspense>
      <Suspense fallback={<p className="text-muted-foreground">Загружаем отзывы…</p>}>
        <MyReviews />
      </Suspense>
    </div>
  );
}

// Сюда форма отзыва приводит с ?sent=1: и новый, и изменённый отзыв уходят на модерацию.
async function SentNotice({ searchParams }: Pick<PageProps<"/me">, "searchParams">) {
  const { sent } = await searchParams;
  if (!sent) return null;

  return (
    <Alert>
      <AlertDescription>Спасибо! Отзыв отправлен на модерацию и появится на странице обзорщика после проверки.</AlertDescription>
    </Alert>
  );
}

async function MyReviews() {
  const api = await userApi();
  const { data, response } = await api.GET("/api/reviews/my");
  // proxy.ts уже увёл гостей без cookie, здесь отсеиваются просроченные токены.
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
        <ReviewCard key={review.id} review={review} showReviewer own />
      ))}
    </div>
  );
}
