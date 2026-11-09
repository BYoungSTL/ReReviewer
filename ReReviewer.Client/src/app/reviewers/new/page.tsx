import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { Suspense } from "react";
import { failed, userApi } from "@/lib/api/client";
import { getGenres } from "@/lib/api/public";
import { SuggestForm } from "./suggest-form";

export const metadata: Metadata = {
  title: "Предложить обзорщика",
  robots: { index: false },
};

export default function NewReviewerPage({ searchParams }: PageProps<"/reviewers/new">) {
  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-6">
      <div className="flex flex-col gap-2">
        <h1 className="text-3xl leading-tight font-bold">Предложить обзорщика</h1>
        <p className="text-muted-foreground">
          Не нашли журналиста или блогера? Расскажите о нём: после проверки модератором профиль появится на сайте,
          и его можно будет оценить.
        </p>
      </div>
      <Suspense fallback={<p className="text-muted-foreground">Загружаем форму…</p>}>
        <Suggest searchParams={searchParams} />
      </Suspense>
    </div>
  );
}

// ?self=1 — пришли из настроек предложить себя: отметка «это я» уже стоит.
async function Suggest({ searchParams }: Pick<PageProps<"/reviewers/new">, "searchParams">) {
  const { self } = await searchParams;
  const api = await userApi();
  const [genres, me] = await Promise.all([getGenres(), api.GET("/api/auth/me")]);
  // proxy.ts уже увёл гостей без cookie, здесь отсеиваются просроченные токены.
  if (me.response.status === 401) redirect("/login?returnTo=/reviewers/new");
  const user = me.data ?? failed(me.response);

  const sorted = [...genres].sort((a, b) => a.title.localeCompare(b.title, "ru"));

  return <SuggestForm genres={sorted} canClaim={user.reviewerId === null} self={self === "1"} />;
}
