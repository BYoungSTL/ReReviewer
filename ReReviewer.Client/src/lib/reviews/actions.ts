"use server";

import { updateTag } from "next/cache";
import { redirect } from "next/navigation";
import { userApi } from "@/lib/api/client";
import { API_UNAVAILABLE, problemMessage } from "@/lib/api/errors";
import { reviewerTag, userTag } from "@/lib/api/public";
import type { ReviewViewModel } from "@/lib/api/schema";
import { MATERIAL_ERRORS } from "@/lib/materials/constants";
import { CRITERION_FIELD, REQUIRED_CRITERION_FIELD } from "./criteria";
import { MAX_NEW_MATERIALS, readNewMaterials, type NewMaterialValues } from "./new-materials";

// Значения формы возвращаются при ошибке: после Server Action React сбрасывает поля к значениям по умолчанию.
// criterionScores: Id критерия → процент от 0 до 100 строкой. newMaterials — материалы, которых у обзорщика ещё нет.
export type ReviewFormValues = {
  text: string;
  score: number | null;
  materialIds: string[];
  criterionScores: Record<string, string>;
  newMaterials: NewMaterialValues[];
};

export type ReviewFormState = {
  error: string;
  values: ReviewFormValues;
} | undefined;

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";
const CRITERIA_MISSING = "Оцените каждый критерий.";

// Новый отзыв о своём профиле форма не показывает, а эту ошибку владелец встретит, правя отзыв, написанный до привязки.
// Оценку каждого критерия проверяет и форма, но набор критериев мог измениться, пока страница была открыта.
// Ошибки новых материалов — как у отдельной заявки на материал, но с подсказкой, что делать в форме отзыва.
const KNOWN_ERRORS: Record<string, string> = {
  ...MATERIAL_ERRORS,
  "A material with this link already exists.": "Материал с одной из ссылок уже есть на сайте. Если он этого обзорщика, отметьте его в списке.",
  "Game not found.": "Выберите игру для каждого нового материала.",
  [`A review can add at most ${MAX_NEW_MATERIALS} new materials.`]: `В отзыв можно добавить не больше ${MAX_NEW_MATERIALS} новых материалов.`,
  "Each new material must have its own link.": "У каждого нового материала должна быть своя ссылка.",
  "You cannot review your own reviewer profile.": "Это ваш профиль обзорщика: оценивать самого себя нельзя, поэтому отзыв о нём не изменить.",
  "Every criterion must be scored.": CRITERIA_MISSING,
  "Criterion score must be between 0 and 100.": "Оценка по критерию — от 0 до 100 %.",
};

export async function createReview(reviewerId: string, _state: ReviewFormState, form: FormData): Promise<ReviewFormState> {
  return saveReview(form, `/reviewers/${reviewerId}/review`, async (body) => {
    const api = await userApi();

    // Новый отзыв ждёт модерации и на странице обзорщика не виден, поэтому её кэш сбрасывать незачем.
    return api.POST("/api/reviews", { body: { ...body, reviewerId } });
  });
}

// Право на правку проверяет API: чужой отзыв он не даст изменить, какой бы id ни пришёл из браузера.
export async function updateReview(reviewId: string, _state: ReviewFormState, form: FormData): Promise<ReviewFormState> {
  return saveReview(form, `/reviews/${reviewId}/edit`, async (body) => {
    const api = await userApi();
    const result = await api.PUT("/api/reviews/{id}", { params: { path: { id: reviewId } }, body });

    // Опубликованный отзыв после правки уходит со страниц обзорщика и автора и перестаёт влиять на рейтинг.
    if (result.data) {
      updateTag(reviewerTag(result.data.reviewerId));
      updateTag(userTag(result.data.userId));
    }

    return result;
  });
}

async function saveReview(
  form: FormData,
  formPath: string,
  send: (body: ReviewViewModel) => Promise<{ error?: unknown; response: Response }>,
): Promise<ReviewFormState> {
  const values = readValues(form);
  const failure = (error: string) => ({ error, values });

  if (!values.text) return failure("Напишите текст отзыва.");
  if (values.score === null) return failure("Поставьте общую оценку от 1 до 10.");
  // Критерии форма помечает скрытыми полями; оценку ставит ползунок, пока его не тронули — её нет.
  if (form.getAll(REQUIRED_CRITERION_FIELD).some((id) => !(String(id) in values.criterionScores)))
    return failure(CRITERIA_MISSING);
  // Остальное о новых материалах (ссылка http(s), игра есть, ссылка не занята) проверяет API.
  if (values.newMaterials.some((material) => !material.title)) return failure("Укажите название каждого нового материала.");
  if (values.newMaterials.some((material) => !material.url)) return failure("Укажите ссылку на каждый новый материал.");
  if (values.newMaterials.some((material) => !material.gameId)) return failure(KNOWN_ERRORS["Game not found."]);

  // Модель запроса общая с ответом (см. README), поэтому здесь есть и поля, которые API при сохранении игнорирует:
  // автора он берёт из токена, а имена, статус и даты заполняет сам. При правке он не меняет и обзорщика.
  const body: ReviewViewModel = {
    id: null,
    userId: EMPTY_GUID,
    reviewerId: EMPTY_GUID,
    text: values.text,
    score: values.score,
    materials: values.materialIds.map((id) => ({ id, title: null, url: null, format: null })),
    criterionScores: Object.entries(values.criterionScores).map(([criterionId, score]) => ({
      criterionId: Number(criterionId),
      score: Number(score),
      criterionTitle: null,
    })),
    newMaterials: values.newMaterials.map((material) => ({
      gameId: material.gameId,
      title: material.title,
      url: material.url,
      format: material.format,
      publishedAt: material.publishedAt || null,
      gameTitle: null,
      status: null,
    })),
    userName: null,
    userReviewerId: null,
    userAvatarUpdatedAt: null,
    reviewerName: null,
    status: null,
    createdAt: new Date().toISOString(),
    updatedAt: null,
  };

  let result: Awaited<ReturnType<typeof send>>;
  try {
    result = await send(body);
  } catch {
    return failure(API_UNAVAILABLE);
  }

  // Токен истёк, пока пользователь заполнял форму. Введённое при этом пропадёт: вход открывает страницу заново.
  if (result.response.status === 401) redirect(`/login?returnTo=${encodeURIComponent(formPath)}`);
  // Проверяется статус, а не error: при ошибке с пустым телом openapi-fetch вернёт в error пустую строку.
  if (!result.response.ok) {
    const message = problemMessage(result.error, "Не удалось сохранить отзыв.");

    return failure(KNOWN_ERRORS[message] ?? message);
  }

  redirect("/me?sent=1");
}

function readValues(form: FormData): ReviewFormValues {
  const score = Number(form.get("score"));
  const criterionScores: Record<string, string> = {};
  for (const [name, value] of form.entries())
    if (name.startsWith(CRITERION_FIELD) && value)
      criterionScores[name.slice(CRITERION_FIELD.length)] = String(value);

  return {
    text: String(form.get("text") ?? "").trim(),
    score: Number.isInteger(score) && score >= 1 && score <= 10 ? score : null,
    materialIds: form.getAll("material").map(String),
    criterionScores,
    newMaterials: readNewMaterials(form),
  };
}
