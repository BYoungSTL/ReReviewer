// Даты — в UTC: сервер и браузер должны отрендерить одинаковую строку, иначе React увидит расхождение при гидратации.
const dateFormat = new Intl.DateTimeFormat("ru-RU", { dateStyle: "long", timeZone: "UTC" });

export const formatDate = (value: string) =>
  dateFormat.format(new Date(value));

// Оценки по шкале 1–10, средние — с одним знаком после запятой.
export const formatScore = (value: number) =>
  value.toLocaleString("ru-RU", { minimumFractionDigits: 1, maximumFractionDigits: 1 });

const STATUS_LABELS: Record<string, string> = {
  Pending: "На модерации",
  Published: "Опубликован",
  Rejected: "Отклонён",
};

export const reviewStatusLabel = (status: string | null) =>
  (status && STATUS_LABELS[status]) ?? status ?? "";

// Заявки на нового обзорщика и привязку профиля.
const REQUEST_STATUS_LABELS: Record<string, string> = {
  Pending: "На рассмотрении",
  Approved: "Одобрена",
  Rejected: "Отклонена",
};

export const requestStatusLabel = (status: string | null) =>
  (status && REQUEST_STATUS_LABELS[status]) ?? status ?? "";

const FORMAT_LABELS: Record<string, string> = {
  Article: "Статья",
  Video: "Видео",
  Stream: "Стрим",
  Podcast: "Подкаст",
};

export const materialFormatLabel = (format: string) =>
  FORMAT_LABELS[format] ?? format;

const ROLE_LABELS: Record<string, string> = {
  Admin: "Администратор",
  Moderator: "Модератор",
};

export const roleLabel = (role: string) =>
  ROLE_LABELS[role] ?? role;

// "1 отзыв", "3 отзыва", "5 отзывов".
export function pluralize(count: number, one: string, few: string, many: string) {
  const mod10 = count % 10;
  const mod100 = count % 100;
  if (mod10 === 1 && mod100 !== 11) return one;
  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return few;

  return many;
}
