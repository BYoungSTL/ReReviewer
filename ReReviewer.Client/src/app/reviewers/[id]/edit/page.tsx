import type { Metadata } from "next";
import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { Suspense } from "react";
import { failed, userApi } from "@/lib/api/client";
import { getGenres, getOutlets, isGuid } from "@/lib/api/public";
import { ReviewerProfileForm } from "./reviewer-profile-form";

export const metadata: Metadata = {
  title: "Редактирование профиля",
  robots: { index: false },
};

export default function EditReviewerPage({ params }: PageProps<"/reviewers/[id]/edit">) {
  return (
    <Suspense fallback={<p className="text-muted-foreground">Загружаем профиль…</p>}>
      <Edit params={params} />
    </Suspense>
  );
}

// Профиль запрашивается без кэша: правят то, что сохранено сейчас. Владельца проверяет и API — он меняет
// профиль, привязанный к токену, так что Id в адресе только выбирает, что показать.
async function Edit({ params }: Pick<PageProps<"/reviewers/[id]/edit">, "params">) {
  const { id } = await params;
  if (!isGuid(id)) notFound();

  const api = await userApi();
  const [me, profile, genres, outlets] = await Promise.all([
    api.GET("/api/auth/me"),
    api.GET("/api/reviewers/{id}", { params: { path: { id } } }),
    getGenres(),
    getOutlets(),
  ]);
  // proxy.ts уже увёл гостей без cookie, здесь отсеиваются просроченные токены.
  if (me.response.status === 401) redirect(`/login?returnTo=${encodeURIComponent(`/reviewers/${id}/edit`)}`);
  if (profile.response.status === 404) notFound();
  const user = me.data ?? failed(me.response);
  const reviewer = profile.data ?? failed(profile.response);

  const profileLink = <Link href={`/reviewers/${id}`} className="text-primary underline-offset-4 hover:underline">{reviewer.fullName}</Link>;

  if (reviewer.userId !== user.id)
    return (
      <div className="mx-auto flex w-full max-w-2xl flex-col gap-4">
        <h1 className="text-3xl leading-tight font-bold">Редактирование профиля</h1>
        <p className="rounded-2xl border bg-card p-5">
          Профиль {profileLink} может править только сам обзорщик, когда его аккаунт привязан к профилю.
          {reviewer.userId === null && user.reviewerId === null && <> Если это вы, <Link href={`/reviewers/${id}/claim`} className="text-primary underline-offset-4 hover:underline">отправьте запрос на привязку</Link>.</>}
        </p>
      </div>
    );

  const sortedGenres = [...genres].sort((a, b) => a.title.localeCompare(b.title, "ru"));
  const sortedOutlets = [...outlets].sort((a, b) => a.name.localeCompare(b.name, "ru"));

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-6">
      <div className="flex flex-col gap-2">
        <h1 className="text-3xl leading-tight font-bold">Редактирование профиля</h1>
        <p className="text-muted-foreground">
          Так ваш профиль {profileLink} видят читатели. Знаки выдаёт администрация сайта, а материалы добавляют модераторы.
        </p>
      </div>
      <ReviewerProfileForm reviewer={reviewer} genres={sortedGenres} outlets={sortedOutlets} />
    </div>
  );
}
