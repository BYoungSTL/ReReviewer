import { cn } from "cn";
import { formatScore } from "@/lib/format";
import { SCORE_BG, scoreTone } from "@/lib/score";

// Оценка на цветной плашке. Средние пишутся с одним знаком после запятой (9,0), оценки отзывов — целыми.
export function ScoreBadge({ score, average = false, size = "md", className }: {
  score: number;
  average?: boolean;
  size?: "sm" | "md" | "lg";
  className?: string;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center justify-center rounded-lg font-display font-bold tabular-nums text-background",
        SCORE_BG[scoreTone(score)],
        size === "sm" && "min-w-8 px-2 py-0.5 text-sm",
        size === "md" && "min-w-11 px-2.5 py-0.5 text-lg",
        size === "lg" && "min-w-16 px-3 py-1 text-2xl",
        className,
      )}
    >
      {average ? formatScore(score) : score}
    </span>
  );
}
