"use server";

import { redirect } from "next/navigation";
import { userApi } from "@/lib/api/client";
import { API_UNAVAILABLE, problemMessage } from "@/lib/api/errors";

// Заявки, которые рассматривают модераторы: новый обзорщик и привязка профиля к своему аккаунту.
// Значения формы возвращаются при ошибке: после Server Action React сбрасывает поля к значениям по умолчанию.

export type SuggestFormValues = {
  fullName: string;
  note: string;
  genreIds: string[];
  isSelf: boolean;
};

export type SuggestFormState = { error: string; values: SuggestFormValues } | undefined;

export type ClaimFormState = { error: string; values: { note: string; reviewerId: string } } | undefined;

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";

// Ошибки, которые пользователь может встретить на этих формах. Остальные API отдаёт по-английски, как есть.
const KNOWN_ERRORS: Record<string, string> = {
  "A reviewer with this name already exists.": "Такой обзорщик уже есть на сайте — найдите его через поиск на главной.",
  "This reviewer has already been suggested and is waiting for moderation.": "Этого обзорщика уже предложили, заявка ждёт модератора.",
  "Your account is already linked to a reviewer profile.": "Ваш аккаунт уже привязан к профилю обзорщика.",
  "You already have a pending request to link a reviewer profile.": "У вас уже есть заявка на привязку профиля. Дождитесь решения модератора.",
  "This reviewer profile is already linked to a user.": "Этот профиль уже привязан к другому аккаунту.",
};

export async function suggestReviewer(_state: SuggestFormState, form: FormData): Promise<SuggestFormState> {
  const values: SuggestFormValues = {
    fullName: text(form, "fullName"),
    note: text(form, "note"),
    genreIds: form.getAll("genre").map(String),
    isSelf: form.get("isSelf") === "on",
  };
  const failure = (error: string) => ({ error, values });

  if (!values.fullName) return failure("Укажите имя или псевдоним обзорщика.");
  if (values.genreIds.length === 0) return failure("Выберите хотя бы один жанр.");

  const error = await submit("/reviewers/new", "Не удалось отправить заявку.", async () => {
    const api = await userApi();

    // Модель запроса общая с ответом, поэтому здесь есть и поля, которые API заполняет сам.
    return api.POST("/api/suggestions", {
      body: {
        id: null,
        fullName: values.fullName,
        note: values.note || null,
        isSelf: values.isSelf,
        genres: values.genreIds.map((id) => ({ id: Number(id), title: "" })),
        userId: EMPTY_GUID,
        userName: null,
        status: null,
        createdAt: new Date().toISOString(),
        decidedAt: null,
        reviewerId: null,
      },
    });
  });
  if (error) return failure(error);

  redirect("/me?request=suggestion");
}

// Из настроек: профиль выбирают в списке, а не открывают со страницы обзорщика.
export async function claimSelectedReviewer(state: ClaimFormState, form: FormData): Promise<ClaimFormState> {
  const reviewerId = text(form, "reviewerId");
  if (!reviewerId) return { error: "Выберите свой профиль в списке.", values: { note: text(form, "note"), reviewerId } };

  return claimReviewer(reviewerId, state, form);
}

export async function claimReviewer(reviewerId: string, _state: ClaimFormState, form: FormData): Promise<ClaimFormState> {
  const values = { note: text(form, "note"), reviewerId };
  const failure = (error: string) => ({ error, values });

  if (!values.note) return failure("Напишите, как модератору убедиться, что это вы.");

  const error = await submit(`/reviewers/${reviewerId}/claim`, "Не удалось отправить запрос.", async () => {
    const api = await userApi();

    return api.POST("/api/claims", {
      body: {
        id: null,
        reviewerId,
        note: values.note,
        reviewerName: null,
        userId: EMPTY_GUID,
        userName: null,
        status: null,
        createdAt: new Date().toISOString(),
        decidedAt: null,
      },
    });
  });
  if (error) return failure(error);

  redirect("/me?request=claim");
}

// Возвращает текст ошибки или undefined при успехе. redirect — вне try: он работает через исключение.
async function submit(formPath: string, fallback: string, send: () => Promise<{ error?: unknown; response: Response }>) {
  let result: { error?: unknown; response: Response };
  try {
    result = await send();
  } catch {
    return API_UNAVAILABLE;
  }

  // Токен истёк, пока пользователь заполнял форму.
  if (result.response.status === 401) redirect(`/login?returnTo=${encodeURIComponent(formPath)}`);
  // Проверяется статус, а не error: при ошибке с пустым телом openapi-fetch вернёт в error пустую строку.
  if (result.response.ok) return undefined;

  const message = problemMessage(result.error, fallback);

  return KNOWN_ERRORS[message] ?? message;
}

const text = (form: FormData, name: string) =>
  String(form.get(name) ?? "").trim();
