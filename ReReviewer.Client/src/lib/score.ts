// Шкала оценок 1–10 в три тона: 8 и выше, от 6 до 8, ниже 6. Цвета — токены --score-* в globals.css.
// Число всегда пишется рядом, поэтому цвет только подсказывает и не несёт смысла в одиночку.
export type ScoreTone = "high" | "mid" | "low";

export const scoreTone = (score: number): ScoreTone =>
  score >= 8 ? "high" : score >= 6 ? "mid" : "low";

// Полные имена классов, чтобы Tailwind их нашёл.
export const SCORE_BG: Record<ScoreTone, string> = {
  high: "bg-score-high",
  mid: "bg-score-mid",
  low: "bg-score-low",
};

export const SCORE_TEXT: Record<ScoreTone, string> = {
  high: "text-score-high",
  mid: "text-score-mid",
  low: "text-score-low",
};
