"use server";

import { refresh, updateTag } from "next/cache";
import { redirect } from "next/navigation";
import { userApi } from "@/lib/api/client";
import { API_UNAVAILABLE, problemMessage } from "@/lib/api/errors";
import { REVIEWS_TAG, reviewerTag } from "@/lib/api/public";

// Действия раздела модерации. Права проверяет API: без нужной роли он ответит 403, что бы ни пришло из браузера.

export type AdminActionState = { error: string } | undefined;

type ApiResult = { error?: unknown; response: Response };

export async function moderateReview(
  reviewId: string,
  reviewerId: string,
  _state: AdminActionState,
  form: FormData,
): Promise<AdminActionState> {
  const approve = form.get("decision") === "approve";

  return run("/admin/reviews", "Не удалось сохранить решение.", async () => {
    const api = await userApi();
    const params = { params: { path: { id: reviewId } } };
    const result = approve
      ? await api.POST("/api/reviews/{id}/approve", params)
      : await api.POST("/api/reviews/{id}/reject", params);

    // Одобренный отзыв появляется на странице обзорщика и меняет его рейтинг. Отклонённый там и не был виден.
    if (approve && result.response.ok) updateTag(reviewerTag(reviewerId));

    return result;
  });
}

// Одобренная заявка создаёт обзорщика, и он сразу появляется в списке на главной.
export async function moderateSuggestion(suggestionId: string, _state: AdminActionState, form: FormData): Promise<AdminActionState> {
  const approve = form.get("decision") === "approve";

  return run("/admin/reviewers", "Не удалось сохранить решение.", async () => {
    const api = await userApi();
    const params = { params: { path: { id: suggestionId } } };
    const result = approve
      ? await api.POST("/api/suggestions/{id}/approve", params)
      : await api.POST("/api/suggestions/{id}/reject", params);

    if (approve && result.response.ok) updateTag("reviewers");

    return result;
  });
}

// После одобрения профиль отмечается как подтверждённый: сбрасывается его кэш.
export async function moderateClaim(
  claimId: string,
  reviewerId: string,
  _state: AdminActionState,
  form: FormData,
): Promise<AdminActionState> {
  const approve = form.get("decision") === "approve";

  return run("/admin/claims", "Не удалось сохранить решение.", async () => {
    const api = await userApi();
    const params = { params: { path: { id: claimId } } };
    const result = approve
      ? await api.POST("/api/claims/{id}/approve", params)
      : await api.POST("/api/claims/{id}/reject", params);

    if (approve && result.response.ok) updateTag(reviewerTag(reviewerId));

    return result;
  });
}

// Одна форма на пользователя, действие выбирает нажатая кнопка (intent).
export async function moderateUser(userId: string, _state: AdminActionState, form: FormData): Promise<AdminActionState> {
  const intent = form.get("intent");
  if (intent !== "roles" && intent !== "block" && intent !== "unblock" && intent !== "delete")
    return { error: "Неизвестное действие." };

  const params = { params: { path: { id: userId } } };

  return run("/admin/users", "Не удалось изменить пользователя.", async () => {
    const api = await userApi();

    switch (intent) {
      case "roles":
        return api.PUT("/api/users/{id}/roles", {
          ...params,
          body: form.getAll("role").map(Number).filter(Number.isInteger),
        });
      case "block":
        return api.POST("/api/users/{id}/block", params);
      case "unblock":
        return api.POST("/api/users/{id}/unblock", params);
      case "delete": {
        const result = await api.DELETE("/api/users/{id}", params);
        // Вместе с пользователем удалены его отзывы, а у каких они обзорщиков, здесь неизвестно: сбрасываются все.
        if (result.response.ok) updateTag(REVIEWS_TAG);

        return result;
      }
    }
  });
}

async function run(returnTo: string, fallback: string, send: () => Promise<ApiResult>): Promise<AdminActionState> {
  let result: ApiResult;
  try {
    result = await send();
  } catch {
    return { error: API_UNAVAILABLE };
  }

  // Токен истёк, или аккаунт модератора заблокировали.
  if (result.response.status === 401) redirect(`/login?returnTo=${encodeURIComponent(returnTo)}`);

  // Список обновляется и после ошибки: например, отзыв мог уже обработать другой модератор.
  refresh();

  if (result.response.status === 403) return { error: "Недостаточно прав. Если роль выдали недавно, выйдите и войдите снова." };
  // Проверяется статус, а не error: при ошибке с пустым телом openapi-fetch вернёт в error пустую строку.
  if (!result.response.ok) return { error: problemMessage(result.error, fallback) };
}
