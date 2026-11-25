"use server";

import { redirect } from "next/navigation";
import { userApi } from "@/lib/api/client";
import { API_UNAVAILABLE, problemMessage } from "@/lib/api/errors";
import { MATERIAL_ERRORS } from "./constants";

// Заявка на новый материал обзорщика. Материал появится на странице обзорщика только после одобрения модератором,
// поэтому публичный кэш здесь не сбрасывается. После успеха форма очищается: можно предложить ещё один материал.

export type MaterialSuggestionValues = {
  title: string;
  url: string;
  gameId: string;
  format: string;
  publishedAt: string;
};

export type MaterialSuggestionState = {
  error?: string;
  success?: string;
  values?: MaterialSuggestionValues;
} | undefined;

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";

export async function suggestMaterial(
  reviewerId: string,
  _state: MaterialSuggestionState,
  form: FormData,
): Promise<MaterialSuggestionState> {
  const values: MaterialSuggestionValues = {
    title: text(form, "title"),
    url: text(form, "url"),
    gameId: text(form, "gameId"),
    format: text(form, "format"),
    publishedAt: text(form, "publishedAt"),
  };
  const failure = (error: string) => ({ error, values });

  if (!values.title) return failure(MATERIAL_ERRORS["Material title must not be empty."]);
  if (!values.url) return failure(MATERIAL_ERRORS["Material link must not be empty."]);
  if (!values.gameId) return failure(MATERIAL_ERRORS["Game not found."]);

  let result: { error?: unknown; response: Response };
  try {
    const api = await userApi();
    // Модель запроса общая с ответом, поэтому здесь есть и поля, которые API заполняет сам.
    result = await api.POST("/api/materialsuggestions", {
      body: {
        id: null,
        reviewerId,
        gameId: values.gameId,
        title: values.title,
        url: values.url,
        format: values.format,
        publishedAt: values.publishedAt || null,
        reviewerName: null,
        gameTitle: null,
        userId: EMPTY_GUID,
        userName: null,
        status: null,
        createdAt: new Date().toISOString(),
        decidedAt: null,
        materialId: null,
      },
    });
  } catch {
    return failure(API_UNAVAILABLE);
  }

  // Токен истёк, пока пользователь заполнял форму.
  if (result.response.status === 401) redirect(`/login?returnTo=${encodeURIComponent(`/reviewers/${reviewerId}/materials/new`)}`);
  if (!result.response.ok) {
    const message = problemMessage(result.error, "Не удалось отправить материал.");

    return failure(MATERIAL_ERRORS[message] ?? message);
  }

  return { success: `Материал «${values.title}» отправлен на проверку. Он появится у обзорщика после одобрения модератором.` };
}

const text = (form: FormData, name: string) =>
  String(form.get(name) ?? "").trim();
