import { cn } from "cn";
import { Award, BadgeCheck, ChevronLeft, FileText, Mic, PenLine, Radio, Video, type LucideIcon } from "lucide-react";
import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Suspense } from "react";
import { Avatar } from "@/components/avatar";
import { buttonVariants } from "@/components/ui/button";
import { getReviewer, getReviewerMaterials, getReviewerRating, getReviewerReviews, isGuid } from "@/lib/api/public";
import type { MaterialViewModel, ReviewerRatingViewModel, ReviewerViewModel } from "@/lib/api/schema";
import { formatDate, formatScore, materialFormatLabel, pluralize } from "@/lib/format";
import { currentOutlets, outletPeriod, pastOutlets } from "@/lib/reviewers";
import { SCORE_BG, SCORE_TEXT, scoreTone } from "@/lib/score";
import { ReviewsList } from "./reviews-list";

const FORMAT_ICONS: Record<string, LucideIcon> = {
  Video,
  Article: FileText,
  Stream: Radio,
  Podcast: Mic,
};

// Главная страница для поиска: «отзывы о <обзорщик>». Заголовок и описание — в метатегах и Open Graph,
// чтобы ссылка в Telegram или Discord разворачивалась в карточку. Данные кэшируются (lib/api/public.ts),
// поэтому generateMetadata и страница не запрашивают API дважды.
export async function generateMetadata({ params }: PageProps<"/reviewers/[id]">): Promise<Metadata> {
  const { id } = await params;
  const reviewer = isGuid(id) ? await getReviewer(id) : null;
  if (!reviewer) return { title: "Обзорщик не найден" };

  const rating = await getReviewerRating(id);
  const description = rating?.reviewCount && rating.averageScore !== null
    ? `${reviewer.fullName}: средняя оценка ${formatScore(rating.averageScore)} из 10 по ${rating.reviewCount} ${pluralize(rating.reviewCount, "отзыву", "отзывам", "отзывам")} на ReReviewer.`
    : `Отзывы о материалах ${reviewer.fullName} на ReReviewer.`;

  return {
    title: reviewer.fullName,
    description,
    openGraph: { title: reviewer.fullName, description, type: "profile" },
  };
}

export default function ReviewerPage({ params }: PageProps<"/reviewers/[id]">) {
  return (
    <Suspense fallback={<p className="text-muted-foreground">Загружаем профиль…</p>}>
      <ReviewerProfile params={params} />
    </Suspense>
  );
}

async function ReviewerProfile({ params }: Pick<PageProps<"/reviewers/[id]">, "params">) {
  const { id } = await params;
  if (!isGuid(id)) notFound();

  const [reviewer, rating, reviews, materials] = await Promise.all([
    getReviewer(id),
    getReviewerRating(id),
    getReviewerReviews(id),
    getReviewerMaterials(id),
  ]);
  if (!reviewer) notFound();

  return (
    <div className="flex flex-col gap-7">
      <Link href="/" className="inline-flex min-h-11 items-center gap-1.5 self-start font-semibold text-muted-foreground hover:text-foreground">
        <ChevronLeft aria-hidden="true" className="size-4" />
        Все обзорщики
      </Link>

      <ProfileHeader id={id} reviewer={reviewer} />

      <div className="flex flex-wrap items-start gap-6">
        <RatingPanel rating={rating} />

        <div className="flex min-w-0 flex-[999_1_560px] flex-col gap-8">
          {materials.length > 0 && <Materials materials={materials} />}

          <section className="flex flex-col gap-3">
            <h2 className="font-display text-xl font-bold">
              Отзывы <span className="font-medium text-muted-foreground">{reviews.totalCount}</span>
            </h2>
            <ReviewsList reviewerId={id} firstPage={reviews} />
          </section>
        </div>
      </div>
    </div>
  );
}

function ProfileHeader({ id, reviewer }: { id: string; reviewer: ReviewerViewModel }) {
  const current = currentOutlets(reviewer);
  const past = pastOutlets(reviewer);

  return (
    <section className="flex flex-wrap items-end justify-between gap-6 rounded-3xl border bg-card p-6 sm:p-7">
      <div className="flex min-w-0 flex-wrap items-center gap-5">
        <Avatar id={id} name={reviewer.fullName} size="lg" />
        <div className="flex min-w-0 flex-col gap-2">
          <h1 className="text-3xl leading-tight font-bold sm:text-4xl">{reviewer.fullName}</h1>
          <p className="text-secondary-foreground/85">
            {current.length > 0
              ? current.map((outlet) => outlet.startedAt
                ? `${outlet.outletName} · с ${outlet.startedAt.slice(0, 4)} года`
                : outlet.outletName).join(", ")
              : "Независимый автор"}
          </p>
          {past.length > 0 && (
            <p className="text-sm text-muted-foreground">
              Раньше: {past.map((outlet) => `${outlet.outletName}, ${outletPeriod(outlet)}`).join("; ")}
            </p>
          )}
          {(reviewer.genres.length > 0 || reviewer.badges.length > 0) && (
            <ul className="flex flex-wrap gap-1.5 pt-1">
              {reviewer.genres.map((genre) => (
                <li key={genre.id} className="rounded-full bg-secondary px-2.5 py-0.5 text-sm text-secondary-foreground/80">{genre.title}</li>
              ))}
              {reviewer.badges.map((badge) => (
                <li key={badge.badgeId} className="inline-flex items-center gap-1.5 rounded-full bg-score-mid/15 px-2.5 py-0.5 text-sm text-score-mid">
                  <Award aria-hidden="true" className="size-3.5" />
                  {badge.badgeTitle}
                </li>
              ))}
            </ul>
          )}
          {/* Привязанный профиль подтверждён модератором (запрос «это я»), к остальным можно отправить такой запрос. */}
          {reviewer.userId ? (
            <p className="inline-flex items-center gap-1.5 text-sm font-semibold text-primary">
              <BadgeCheck aria-hidden="true" className="size-4" />
              Профиль подтверждён автором
            </p>
          ) : (
            <Link href={`/reviewers/${id}/claim`} className="self-start text-sm text-muted-foreground underline-offset-4 hover:text-foreground hover:underline">
              Это вы? Привяжите профиль к своему аккаунту
            </Link>
          )}
        </div>
      </div>
      {/* Для гостей ссылка ведёт на вход, а после него — обратно к форме (см. proxy.ts). */}
      <Link href={`/reviewers/${id}/review`} className={cn(buttonVariants({ size: "lg" }), "h-12 gap-2 px-5 text-base font-bold")}>
        <PenLine aria-hidden="true" className="size-4" />
        Написать отзыв
      </Link>
    </section>
  );
}

function RatingPanel({ rating }: { rating: ReviewerRatingViewModel | null }) {
  const score = rating && rating.reviewCount > 0 ? rating.averageScore : null;

  return (
    <aside aria-label="Рейтинг" className="flex flex-[1_1_320px] flex-col gap-6 rounded-3xl border bg-card p-6">
      {rating === null || score === null ? (
        <div className="flex flex-col gap-2">
          <span className="font-display text-2xl font-bold">Оценок пока нет</span>
          <p className="text-muted-foreground">Рейтинг появится, когда опубликуют первый отзыв.</p>
        </div>
      ) : (
        <>
          <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
            <span className={cn("font-display text-6xl leading-none font-bold tabular-nums", SCORE_TEXT[scoreTone(score)])}>
              {formatScore(score)}
            </span>
            <span className="text-muted-foreground">
              из 10 · {rating.reviewCount} {pluralize(rating.reviewCount, "отзыв", "отзыва", "отзывов")}
            </span>
          </div>
          <Distribution scores={rating.scores} />
          {rating.criteria.length > 0 && <Criteria criteria={rating.criteria} />}
        </>
      )}
    </aside>
  );
}

function Distribution({ scores }: { scores: ReviewerRatingViewModel["scores"] }) {
  const max = Math.max(1, ...scores.map((s) => s.count));
  const description = scores
    .filter((s) => s.count > 0)
    .map((s) => `оценка ${s.score} — ${s.count} ${pluralize(s.count, "отзыв", "отзыва", "отзывов")}`)
    .join(", ");

  return (
    <div className="flex flex-col gap-2">
      <h2 className="text-sm font-bold tracking-wider text-secondary-foreground/80 uppercase">Распределение оценок</h2>
      <div role="img" aria-label={`Распределение оценок: ${description}`} className="grid grid-cols-10 grid-rows-[4.5rem] items-end gap-1.5">
        {scores.map((s) => (
          <div
            key={s.score}
            className={cn("rounded", s.count > 0 ? SCORE_BG[scoreTone(s.score)] : "bg-input")}
            style={{ height: s.count > 0 ? `${Math.max(15, (s.count / max) * 100)}%` : "4px" }}
          />
        ))}
      </div>
      <div aria-hidden="true" className="grid grid-cols-10 gap-1.5 text-center text-xs text-muted-foreground tabular-nums">
        {scores.map((s) => <span key={s.score}>{s.score}</span>)}
      </div>
    </div>
  );
}

function Criteria({ criteria }: { criteria: ReviewerRatingViewModel["criteria"] }) {
  const sorted = [...criteria].sort((a, b) => b.averageScore - a.averageScore);

  return (
    <div className="flex flex-col gap-4">
      <h2 className="text-sm font-bold tracking-wider text-secondary-foreground/80 uppercase">По критериям</h2>
      {sorted.map((criterion) => (
        <div key={criterion.criterionId} className="flex flex-col gap-1.5">
          <div className="flex justify-between gap-3">
            <span>{criterion.title}</span>
            <span className="shrink-0 font-bold tabular-nums">
              {formatScore(criterion.averageScore)}{" "}
              <span className="text-sm font-medium text-muted-foreground">
                · {criterion.scoreCount} {pluralize(criterion.scoreCount, "оценка", "оценки", "оценок")}
              </span>
            </span>
          </div>
          <div className="h-2 rounded-full bg-border">
            <div
              className={cn("h-2 rounded-full", SCORE_BG[scoreTone(criterion.averageScore)])}
              style={{ width: `${criterion.averageScore * 10}%` }}
            />
          </div>
        </div>
      ))}
    </div>
  );
}

function Materials({ materials }: { materials: MaterialViewModel[] }) {
  return (
    <section className="flex flex-col gap-3">
      <h2 className="font-display text-xl font-bold">
        Материалы <span className="font-medium text-muted-foreground">{materials.length}</span>
      </h2>
      <ul className="flex flex-col gap-2">
        {materials.map((material) => {
          const Icon = FORMAT_ICONS[material.format] ?? FileText;

          return (
            <li key={material.id}>
              <a
                href={material.url}
                target="_blank"
                rel="noopener noreferrer nofollow"
                className="flex items-center gap-3.5 rounded-2xl border bg-card px-4 py-3.5 transition-colors hover:border-primary/60"
              >
                <span aria-hidden="true" className="flex size-10 shrink-0 items-center justify-center rounded-xl bg-secondary text-secondary-foreground/80">
                  <Icon className="size-5" />
                </span>
                <span className="flex min-w-0 flex-col">
                  <span className="font-semibold break-words">{material.title}</span>
                  <span className="text-sm text-muted-foreground">
                    {[
                      materialFormatLabel(material.format),
                      material.gameTitle,
                      material.outletName,
                      material.publishedAt && formatDate(material.publishedAt),
                    ].filter(Boolean).join(" · ")}
                  </span>
                </span>
              </a>
            </li>
          );
        })}
      </ul>
    </section>
  );
}
