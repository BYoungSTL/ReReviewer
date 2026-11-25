// Ошибки API о предложенном материале — общие для отдельной заявки (lib/materials/actions.ts) и нового материала
// в отзыве (lib/reviews/actions.ts). Отдельным модулем: из файла с "use server" константы не экспортировать.
export const MATERIAL_ERRORS: Record<string, string> = {
  "Material title must not be empty.": "Укажите название материала.",
  "Material title must be at most 250 characters long.": "Название материала — не длиннее 250 символов.",
  "Material link must not be empty.": "Укажите ссылку на материал.",
  "Material link must be at most 800 characters long.": "Ссылка на материал — не длиннее 800 символов.",
  "Material link must be an http or https address.": "Ссылка на материал должна начинаться с http:// или https://.",
  "A material with this link already exists.": "Материал с этой ссылкой уже есть на сайте.",
  "This material has already been suggested and is waiting for moderation.": "Этот материал уже предложили, заявка ждёт модератора.",
  "Game not found.": "Выберите игру из списка.",
};

// Порядок — как в выпадающих списках форм.
export const MATERIAL_FORMATS = ["Video", "Article", "Stream", "Podcast"];
