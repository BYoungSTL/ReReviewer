import type { Metadata } from "next";
import { notFound, redirect } from "next/navigation";
import { Suspense } from "react";
import { Avatar } from "@/components/avatar";
import { ReviewsList } from "@/components/reviews-list";
import { getPublicUser, getUserReviews, isGuid } from "@/lib/api/public";
import { avatarUrl } from "@/lib/avatar";
import { formatDate } from "@/lib/format";

// Публичная страница пользователя: имя, аватарка и опубликованные отзывы. Почты и ролей API здесь не отдаёт.
// Аккаунт, привязанный модератором к профилю обзорщика, отдельной страницы не имеет: его профиль — страница
// обзорщика, а свои отзывы там на вкладке «От обзорщика». Туда эта страница и переадресует.
// В поиск страница не попадает: искать стоит обзорщиков, а не участников сайта.
export async function generateMetadata({ params }: PageProps<"/users/[id]">): Promise<Metadata> {
  const { id } = await params;
  const user = isGuid(id) ? await getPublicUser(id) : null;

  return {
    title: user?.name ?? "Пользователь не найден",
    robots: { index: false },
  };
}

export default function UserPage({ params }: PageProps<"/users/[id]">) {
  return (
    <Suspense fallback={<p className="text-muted-foreground">Загружаем профиль…</p>}>
      <UserProfile params={params} />
    </Suspense>
  );
}

async function UserProfile({ params }: Pick<PageProps<"/users/[id]">, "params">) {
  const { id } = await params;
  if (!isGuid(id)) notFound();

  const [user, reviews] = await Promise.all([getPublicUser(id), getUserReviews(id)]);
  if (!user) notFound();
  if (user.reviewerId) redirect(`/reviewers/${user.reviewerId}?tab=written`);

  return (
    <div className="flex flex-col gap-7">
      <section className="flex flex-wrap items-center gap-5 rounded-3xl border bg-card p-6 sm:p-7">
        <Avatar id={user.id} name={user.name} src={avatarUrl(user.id, user.avatarUpdatedAt)} size="lg" />
        <div className="flex min-w-0 flex-col gap-2">
          <h1 className="text-3xl leading-tight font-bold break-words sm:text-4xl">{user.name}</h1>
          <p className="text-secondary-foreground/85">На ReReviewer с {formatDate(user.createdAt)}</p>
        </div>
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="font-display text-xl font-bold">
          Отзывы <span className="font-medium text-muted-foreground">{reviews.totalCount}</span>
        </h2>
        <ReviewsList source={{ kind: "user", id }} firstPage={reviews} />
      </section>
    </div>
  );
}
