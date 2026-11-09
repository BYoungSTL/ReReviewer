import type { Metadata } from "next";
import Link from "next/link";
import { redirect } from "next/navigation";
import { Suspense } from "react";
import { ReviewCard } from "@/components/review-card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { failed, userApi } from "@/lib/api/client";
import { formatDate, requestStatusLabel } from "@/lib/format";

export const metadata: Metadata = {
  title: "Мои отзывы",
  robots: { index: false },
};

// Формы отзыва, нового обзорщика и привязки профиля приводят сюда и говорят, что отправили.
const NOTICES: Record<string, string> = {
  review: "Спасибо! Отзыв отправлен на модерацию и появится на странице обзорщика после проверки.",
  suggestion: "Заявка отправлена. Обзорщик появится на сайте после проверки модератором.",
  claim: "Запрос отправлен. После проверки модератором профиль привяжется к вашему аккаунту.",
};

export default function MyReviewsPage({ searchParams }: PageProps<"/me">) {
  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-3xl leading-tight font-bold">Мои отзывы</h1>
      <Suspense>
        <SentNotice searchParams={searchParams} />
      </Suspense>
      <Suspense fallback={<p className="text-muted-foreground">Загружаем отзывы…</p>}>
        <MyReviews />
      </Suspense>
      <Suspense>
        <MyRequests />
      </Suspense>
    </div>
  );
}

// Форма отзыва приводит с ?sent=1 (и новый, и изменённый отзыв уходят на модерацию), заявки — с ?request=.
async function SentNotice({ searchParams }: Pick<PageProps<"/me">, "searchParams">) {
  const { sent, request } = await searchParams;
  const notice = sent ? NOTICES.review : typeof request === "string" ? NOTICES[request] : undefined;
  if (!notice) return null;

  return (
    <Alert>
      <AlertDescription>{notice}</AlertDescription>
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
        Вы ещё не написали ни одного отзыва. Выберите обзорщика на <Link href="/" className="text-primary underline underline-offset-4">главной</Link>.
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

// Заявки на новых обзорщиков и запросы на привязку профиля — с решением модератора. Раздела нет, пока заявок нет.
async function MyRequests() {
  const api = await userApi();
  const [suggestions, claims] = await Promise.all([api.GET("/api/suggestions/my"), api.GET("/api/claims/my")]);
  // Просроченный токен уже обработал MyReviews.
  if (suggestions.response.status === 401) return null;
  const items = [
    ...(suggestions.data ?? failed(suggestions.response)).map((s) => ({
      id: s.id,
      createdAt: s.createdAt,
      status: s.status,
      title: <>Новый обзорщик: {s.fullName}{s.isSelf && " (это я)"}</>,
      link: s.status === "Approved" && s.reviewerId ? `/reviewers/${s.reviewerId}` : null,
    })),
    ...(claims.data ?? failed(claims.response)).map((c) => ({
      id: c.id,
      createdAt: c.createdAt,
      status: c.status,
      title: <>Привязка профиля: {c.reviewerName}</>,
      link: `/reviewers/${c.reviewerId}`,
    })),
  ].sort((a, b) => b.createdAt.localeCompare(a.createdAt));

  if (items.length === 0) return null;

  return (
    <section className="flex flex-col gap-3 pt-4">
      <h2 className="font-display text-xl font-bold">Мои заявки</h2>
      <ul className="flex flex-col gap-2">
        {items.map((item) => (
          <li key={item.id} className="flex flex-wrap items-center justify-between gap-x-4 gap-y-1 rounded-2xl border bg-card px-5 py-4">
            <span className="flex flex-col">
              <span className="font-semibold">{item.title}</span>
              <span className="text-sm text-muted-foreground">{formatDate(item.createdAt)} · {requestStatusLabel(item.status)}</span>
            </span>
            {item.link && (
              <Link href={item.link} className="text-sm font-semibold text-primary underline-offset-4 hover:underline">
                Открыть профиль
              </Link>
            )}
          </li>
        ))}
      </ul>
    </section>
  );
}
