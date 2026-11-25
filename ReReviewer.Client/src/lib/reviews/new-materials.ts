// Новые материалы в форме отзыва: те, которых у обзорщика ещё нет. Модератор проверяет их вместе с отзывом
// и при одобрении отзыва добавляет обзорщику. Каждое поле строки повторяется под одним именем, поэтому
// form.getAll() отдаёт значения строк по порядку: у пустой даты тоже есть значение (""), и строки не съезжают.
// Отдельным модулем: из файла с "use server" константы не экспортировать.
export const NEW_MATERIAL_FIELD = {
  title: "newMaterialTitle",
  url: "newMaterialUrl",
  gameId: "newMaterialGameId",
  format: "newMaterialFormat",
  publishedAt: "newMaterialPublishedAt",
} as const;

// Как MaxNewMaterials в ReviewService.
export const MAX_NEW_MATERIALS = 5;

export type NewMaterialValues = Record<keyof typeof NEW_MATERIAL_FIELD, string>;

export const EMPTY_NEW_MATERIAL: NewMaterialValues = { title: "", url: "", gameId: "", format: "Video", publishedAt: "" };

export function readNewMaterials(form: FormData): NewMaterialValues[] {
  const column = (name: string) => form.getAll(name).map((value) => String(value).trim());
  const titles = column(NEW_MATERIAL_FIELD.title);
  const urls = column(NEW_MATERIAL_FIELD.url);
  const gameIds = column(NEW_MATERIAL_FIELD.gameId);
  const formats = column(NEW_MATERIAL_FIELD.format);
  const dates = column(NEW_MATERIAL_FIELD.publishedAt);

  return titles.map((title, index) => ({
    title,
    url: urls[index] ?? "",
    gameId: gameIds[index] ?? "",
    format: formats[index] ?? "",
    publishedAt: dates[index] ?? "",
  }));
}
