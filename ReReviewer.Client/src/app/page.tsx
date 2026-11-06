import Link from "next/link";
import { connection } from "next/server";
import { Suspense } from "react";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { getReviewers } from "@/lib/api/public";

export default function HomePage() {
  return (
    <div className="flex flex-col gap-8">
      <section className="flex flex-col gap-2">
        <h1 className="text-3xl font-semibold tracking-tight">Обзоры на обзорщиков</h1>
        <p className="max-w-2xl text-muted-foreground">
          Журналисты и блогеры оценивают игры, а здесь оценивают их самих: насколько честны,
          глубоки и полезны их обзоры.
        </p>
      </section>
      <Suspense fallback={<p className="text-muted-foreground">Загружаем обзорщиков…</p>}>
        <ReviewerGrid />
      </Suspense>
    </div>
  );
}

async function ReviewerGrid() {
  // Список запрашивается во время запроса, а не при сборке: на next build API может быть недоступен
  // (в Docker его ещё нет). Между запросами он всё равно кэшируется — см. getReviewers.
  await connection();
  const reviewers = await getReviewers();

  if (reviewers.length === 0)
    return <p className="text-muted-foreground">Обзорщиков пока нет.</p>;

  return (
    <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {reviewers.map((reviewer) => (
        <li key={reviewer.id}>
          <Link href={`/reviewers/${reviewer.id}`} className="block h-full rounded-xl">
            <Card className="h-full transition-colors hover:bg-muted/50">
              <CardHeader>
                <CardTitle>{reviewer.fullName}</CardTitle>
                <CardDescription>
                  {reviewer.outlets.map((outlet) => outlet.outletName).join(", ") || "Независимый автор"}
                </CardDescription>
              </CardHeader>
              {reviewer.genres.length > 0 && (
                <CardContent className="text-muted-foreground">
                  {reviewer.genres.map((genre) => genre.title).join(" · ")}
                </CardContent>
              )}
            </Card>
          </Link>
        </li>
      ))}
    </ul>
  );
}
