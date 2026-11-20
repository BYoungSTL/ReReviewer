import type { CriterionViewModel, ReviewCriterionScoreViewModel, ReviewerViewModel } from "@/lib/api/schema";

// Критерий без жанра подходит любому обзорщику, с жанром — только тем, кто в этом жанре работает.
// Критерии, по которым в отзыве уже стоит оценка, остаются всегда: обзорщик мог сменить жанры,
// а без поля в форме правка удалила бы такую оценку.
export function reviewCriteria(
  criteria: CriterionViewModel[],
  reviewer: ReviewerViewModel,
  scores: ReviewCriterionScoreViewModel[] = [],
) {
  const genreIds = reviewer.genres.map((genre) => genre.id);

  return criteria.filter((criterion) =>
    criterion.genreId === null
    || genreIds.includes(criterion.genreId)
    || scores.some((score) => score.criterionId === criterion.id));
}
