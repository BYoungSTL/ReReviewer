// Поля формы отзыва: оценка критерия — criterion-<Id> (процент от 0 до 100), пометка критерия — requiredCriterion.
// Все критерии обязательны: по пометкам Server Action проверяет, что каждый оценён (ползунок, который не трогали,
// ничего не отправляет). Отдельным модулем, а не в lib/reviews/actions.ts: из файла с "use server" константы не экспортировать.
export const CRITERION_FIELD = "criterion-";
export const REQUIRED_CRITERION_FIELD = "requiredCriterion";
