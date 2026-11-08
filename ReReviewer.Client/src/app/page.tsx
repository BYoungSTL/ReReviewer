import { Award } from "lucide-react";
import Form from "next/form";
import Link from "next/link";
import { Suspense } from "react";
import { Avatar } from "@/components/avatar";
import { ScoreBadge } from "@/components/score-badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getReviewers } from "@/lib/api/public";
import type { ReviewerViewModel } from "@/lib/api/schema";
import { pluralize } from "@/lib/format";
import { outletLine } from "@/lib/reviewers";
import { SortSelect } from "./sort-select";

const SORTS = {
  rating: "По рейтингу",
  reviews: "По числу отзывов",
  name: "По имени",
};
type Sort = keyof typeof SORTS;

// Без оценок — в конце: их рейтинг неизвестен, а не нулевой.
const COMPARE: Record<Sort, (a: ReviewerViewModel, b: ReviewerViewModel) => number> = {
  rating: (a, b) =>
    (b.averageScore ?? -1) - (a.averageScore ?? -1) || b.reviewCount - a.reviewCount || byName(a, b),
  reviews: (a, b) =>
    b.reviewCount - a.reviewCount || (b.averageScore ?? -1) - (a.averageScore ?? -1) || byName(a, b),
  name: (a, b) =>
    byName(a, b),
};

export default function HomePage({ searchParams }: PageProps<"/">) {
  return (
    <div className="flex flex-col gap-8">
      <section className="flex flex-col gap-4 pt-4 sm:pt-8">
        <p className="text-sm font-bold tracking-widest text-primary uppercase">Рейтинг игровых обзорщиков</p>
        <h1 className="max-w-3xl text-4xl leading-tight font-bold sm:text-5xl">Кому верить, когда читаешь обзоры игр</h1>
        <p className="max-w-2xl text-lg text-muted-foreground">
          Игроки оценивают журналистов и блогеров: насколько честны их оценки, глубоки разборы и удобна подача.
        </p>
      </section>
      <Suspense fallback={<p className="text-muted-foreground">Загружаем обзорщиков…</p>}>
        <Reviewers searchParams={searchParams} />
      </Suspense>
    </div>
  );
}

// Обзорщиков немного, и список целиком кэшируется (getReviewers), поэтому поиск, жанр и сортировка — здесь,
// по параметрам адреса: ссылкой на отфильтрованный список можно поделиться.
async function Reviewers({ searchParams }: Pick<PageProps<"/">, "searchParams">) {
  const params = await searchParams;
  const query = param(params.q);
  const genre = param(params.genre);
  const sortParam = param(params.sort);
  const sort: Sort = Object.hasOwn(SORTS, sortParam) ? (sortParam as Sort) : "rating";

  const reviewers = await getReviewers();
  const genres = [...new Set(reviewers.flatMap((reviewer) => reviewer.genres.map((g) => g.title)))]
    .sort((a, b) => a.localeCompare(b, "ru"));
  const reviewCount = reviewers.reduce((sum, reviewer) => sum + reviewer.reviewCount, 0);

  const needle = query.toLocaleLowerCase("ru");
  const shown = reviewers
    .filter((reviewer) => !genre || reviewer.genres.some((g) => g.title === genre))
    .filter((reviewer) => !needle || [reviewer.fullName, ...reviewer.outlets.map((o) => o.outletName ?? "")]
      .some((text) => text.toLocaleLowerCase("ru").includes(needle)))
    .sort(COMPARE[sort]);

  const href = (changes: { genre?: string }) => {
    const next = new URLSearchParams();
    if (query) next.set("q", query);
    const nextGenre = "genre" in changes ? changes.genre : genre;
    if (nextGenre) next.set("genre", nextGenre);
    if (sort !== "rating") next.set("sort", sort);
    const search = next.toString();

    return search ? `/?${search}` : "/";
  };

  return (
    <div className="flex flex-col gap-6">
      <Form action="/" className="flex max-w-3xl flex-wrap items-end gap-3">
        <div className="flex min-w-60 flex-1 flex-col gap-2">
          <Label htmlFor="q" className="sr-only">Поиск обзорщика</Label>
          <Input key={query} id="q" name="q" type="search" defaultValue={query} placeholder="Имя или издание" className="h-11" />
        </div>
        {genre && <input type="hidden" name="genre" value={genre} />}
        <div className="flex items-center gap-2">
          <Label htmlFor="sort" className="text-muted-foreground">Сортировка</Label>
          <SortSelect value={sort} options={SORTS} />
        </div>
        <Button type="submit" size="lg" className="h-11 px-5">Найти</Button>
      </Form>

      <nav aria-label="Жанры" className="flex flex-wrap gap-2">
        {[{ title: "Все", value: undefined }, ...genres.map((title) => ({ title, value: title }))].map((chip) => {
          const active = (chip.value ?? "") === genre;

          return (
            <Link
              key={chip.title}
              href={href({ genre: chip.value })}
              aria-current={active ? "true" : undefined}
              className={active
                ? "inline-flex min-h-11 items-center rounded-full border border-primary bg-primary px-4 font-bold text-primary-foreground"
                : "inline-flex min-h-11 items-center rounded-full border border-input px-4 font-semibold text-secondary-foreground/80 hover:border-primary/60"}
            >
              {chip.title}
            </Link>
          );
        })}
      </nav>

      <p className="text-sm text-muted-foreground">
        {reviewers.length} {pluralize(reviewers.length, "обзорщик", "обзорщика", "обзорщиков")} ·{" "}
        {reviewCount} {pluralize(reviewCount, "опубликованный отзыв", "опубликованных отзыва", "опубликованных отзывов")}
        {(query || genre) && <> · найдено {shown.length}</>}
      </p>

      {shown.length === 0 ? (
        <p className="text-muted-foreground">
          Никого не нашлось. <Link href="/" className="text-primary underline underline-offset-4">Сбросить фильтры</Link>
        </p>
      ) : (
        <ul className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {shown.map((reviewer) => (
            <li key={reviewer.id}>
              <ReviewerCard reviewer={reviewer} />
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function ReviewerCard({ reviewer }: { reviewer: ReviewerViewModel }) {
  const score = reviewer.reviewCount > 0 ? reviewer.averageScore : null;

  return (
    <Link
      href={`/reviewers/${reviewer.id}`}
      className="flex h-full flex-col gap-4 rounded-2xl border bg-card p-5 transition-colors hover:border-primary/60"
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <Avatar id={reviewer.id ?? reviewer.fullName} name={reviewer.fullName} />
          <div className="min-w-0">
            <div className="text-[17px] leading-snug font-bold">{reviewer.fullName}</div>
            <div className="text-sm text-muted-foreground">{outletLine(reviewer)}</div>
          </div>
        </div>
        {score !== null ? (
          <div className="shrink-0 text-right">
            <ScoreBadge score={score} average />
            <div className="mt-1 text-xs text-muted-foreground">
              {reviewer.reviewCount} {pluralize(reviewer.reviewCount, "отзыв", "отзыва", "отзывов")}
            </div>
          </div>
        ) : (
          <span className="shrink-0 rounded-lg border border-dashed border-input px-2.5 py-1 text-xs font-semibold text-muted-foreground">
            Нет оценок
          </span>
        )}
      </div>

      {reviewer.genres.length > 0 && (
        <ul className="flex flex-wrap gap-1.5">
          {reviewer.genres.map((g) => (
            <li key={g.id} className="rounded-full bg-secondary px-2.5 py-0.5 text-sm text-secondary-foreground/80">{g.title}</li>
          ))}
        </ul>
      )}

      {score !== null ? (
        reviewer.badges.length > 0 && (
          <ul className="mt-auto flex flex-wrap gap-x-3 gap-y-1 text-sm text-secondary-foreground/80">
            {reviewer.badges.map((badge) => (
              <li key={badge.badgeId} className="inline-flex items-center gap-1.5">
                <Award aria-hidden="true" className="size-3.5 text-score-mid" />
                {badge.badgeTitle}
              </li>
            ))}
          </ul>
        )
      ) : (
        <span className="mt-auto text-sm font-bold text-primary">Станьте первым, кто оценит</span>
      )}
    </Link>
  );
}

const param = (value: string | string[] | undefined) =>
  typeof value === "string" ? value.trim() : "";

const byName = (a: ReviewerViewModel, b: ReviewerViewModel) =>
  a.fullName.localeCompare(b.fullName, "ru");
