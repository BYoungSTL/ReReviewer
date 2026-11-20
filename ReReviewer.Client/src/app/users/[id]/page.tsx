import { BadgeCheck } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Suspense } from "react";
import { Avatar } from "@/components/avatar";
import { ReviewsList } from "@/components/reviews-list";
import { getPublicUser, getReviewer, getUserReviews, isGuid } from "@/lib/api/public";
import { avatarUrl } from "@/lib/avatar";
import { formatDate } from "@/lib/format";

// Публичная страница пользователя: имя, аватарка, подтверждённый профиль обзорщика и опубликованные отзывы.
// Сюда ведёт шапка подтверждённого профиля обзорщика. Почты и ролей API здесь не отдаёт.
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
  const reviewer = user.reviewerId ? await getReviewer(user.reviewerId) : null;

  return (
    <div className="flex flex-col gap-7">
      <section className="flex flex-wrap items-center gap-5 rounded-3xl border bg-card p-6 sm:p-7">
        <Avatar id={user.id} name={user.name} src={avatarUrl(user.id, user.avatarUpdatedAt)} size="lg" />
        <div className="flex min-w-0 flex-col gap-2">
          <h1 className="text-3xl leading-tight font-bold break-words sm:text-4xl">{user.name}</h1>
          <p className="text-secondary-foreground/85">На ReReviewer с {formatDate(user.createdAt)}</p>
          {reviewer && (
            <Link
              href={`/reviewers/${user.reviewerId}`}
              className="inline-flex items-center gap-1.5 self-start text-sm font-semibold text-primary hover:underline"
            >
              <BadgeCheck aria-hidden="true" className="size-4" />
              Подтверждённый профиль обзорщика: {reviewer.fullName}
            </Link>
          )}
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
