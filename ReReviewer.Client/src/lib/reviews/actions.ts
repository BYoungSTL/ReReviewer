"use server";

import { updateTag } from "next/cache";
import { redirect } from "next/navigation";
import { userApi } from "@/lib/api/client";
import { API_UNAVAILABLE, problemMessage } from "@/lib/api/errors";
import { reviewerTag } from "@/lib/api/public";
import type { ReviewViewModel } from "@/lib/api/schema";

// Значения формы возвращаются при ошибке: после Server Action React сбрасывает поля к значениям по умолчанию.
export type ReviewFormValues = {
  text: string;
  score: number | null;
  materialIds: string[];
  criterionScores: Record<string, string>;
};

export type ReviewFormState = {
  error: string;
  values: ReviewFormValues;
} | undefined;

const CRITERION_FIELD = "criterion-";
const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";

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

    // Опубликованный отзыв после правки уходит со страницы обзорщика и перестаёт влиять на его рейтинг.
    if (result.data) updateTag(reviewerTag(result.data.reviewerId));

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
    userName: null,
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
  if (!result.response.ok) return failure(problemMessage(result.error, "Не удалось сохранить отзыв."));

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
  };
}
