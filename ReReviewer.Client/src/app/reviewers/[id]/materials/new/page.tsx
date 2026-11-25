import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Suspense } from "react";
import { getGameOptions, getReviewer, isGuid } from "@/lib/api/public";
import { MaterialSuggestionForm } from "./material-suggestion-form";

export const metadata: Metadata = {
  title: "Предложить материал",
  robots: { index: false },
};

export default function NewMaterialPage({ params }: PageProps<"/reviewers/[id]/materials/new">) {
  return (
    <Suspense fallback={<p className="text-muted-foreground">Загружаем форму…</p>}>
      <NewMaterial params={params} />
    </Suspense>
  );
}

// Сюда ведёт блок «Материалы» на странице обзорщика. В форме отзыва новый материал добавляют прямо в отзыв.
// Гостей без cookie proxy.ts уводит на вход; просроченный токен Server Action обработает при отправке.
async function NewMaterial({ params }: Pick<PageProps<"/reviewers/[id]/materials/new">, "params">) {
  const { id } = await params;
  if (!isGuid(id)) notFound();

  const [reviewer, games] = await Promise.all([getReviewer(id), getGameOptions()]);
  if (!reviewer) notFound();

  const profileLink = <Link href={`/reviewers/${id}`} className="text-primary underline-offset-4 hover:underline">{reviewer.fullName}</Link>;

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-6">
      <div className="flex flex-col gap-2">
        <h1 className="text-3xl leading-tight font-bold">Предложить материал</h1>
        <p className="text-muted-foreground">
          Статья, видео, стрим или подкаст, который сделал {profileLink}. После проверки модератором материал появится
          на странице обзорщика, и его можно будет отметить в отзыве.
        </p>
      </div>
      <MaterialSuggestionForm reviewerId={id} games={games} />
    </div>
  );
}
