"use server";

import { updateTag } from "next/cache";
import { redirect } from "next/navigation";
import { userApi } from "@/lib/api/client";
import { API_UNAVAILABLE, problemMessage } from "@/lib/api/errors";
import { reviewerTag } from "@/lib/api/public";
import type { ReviewerProfileViewModel } from "@/lib/api/schema";

// Профиль обзорщика, который правит сам владелец подтверждённого аккаунта. Какой профиль менять, API решает
// по токену, поэтому Id из браузера здесь не нужен.

export type ReviewerProfileState = { error?: string; success?: string } | undefined;

const EMPTY_GUID = "00000000-0000-0000-0000-000000000000";

const KNOWN_ERRORS: Record<string, string> = {
  "Your account is not linked to a reviewer profile.": "Ваш аккаунт не привязан к профилю обзорщика.",
  "Reviewer name must not be empty.": "Укажите имя.",
  "A reviewer with this name already exists.": "Обзорщик с таким именем уже есть на сайте.",
  "At least one genre is required.": "Выберите хотя бы один жанр.",
  "About must be at most 1000 characters long.": "«О себе» — не больше 1000 символов.",
  "At most 5 links are allowed.": "Можно указать не больше 5 ссылок.",
  "Link must be at most 300 characters long.": "Ссылка должна быть не длиннее 300 символов.",
  "Links must be http or https addresses.": "Ссылки должны начинаться с http:// или https://.",
  "Each outlet can be listed only once.": "Каждое издание можно указать только один раз.",
  "An outlet's end date cannot be earlier than its start date.": "Дата окончания работы не может быть раньше даты начала.",
};

export async function updateReviewerProfile(_state: ReviewerProfileState, form: FormData): Promise<ReviewerProfileState> {
  const fullName = String(form.get("fullName") ?? "").trim();
  const about = String(form.get("about") ?? "").trim();
  const genreIds = form.getAll("genre").map(Number).filter(Number.isInteger);
  const links = form.getAll("link").map((link) => String(link).trim()).filter(Boolean);

  // Строки мест работы приходят параллельными списками; строки без выбранного издания пропускаются.
  const starts = form.getAll("outletStart");
  const ends = form.getAll("outletEnd");
  const outlets = form.getAll("outletId")
    .map((outletId, index) => ({
      outletId: String(outletId),
      startedAt: String(starts[index] ?? "") || null,
      endedAt: String(ends[index] ?? "") || null,
    }))
    .filter((outlet) => outlet.outletId);

  if (!fullName) return { error: KNOWN_ERRORS["Reviewer name must not be empty."] };
  if (genreIds.length === 0) return { error: KNOWN_ERRORS["At least one genre is required."] };

  const body: ReviewerProfileViewModel = {
    fullName,
    about: about || null,
    links,
    genres: genreIds.map((id) => ({ id, title: "" })),
    outlets: outlets.map((outlet) => ({ ...outlet, reviewerId: EMPTY_GUID, reviewerName: null, outletName: null })),
  };

  let result;
  try {
    const api = await userApi();
    result = await api.PUT("/api/users/me/reviewer", { body });
  } catch {
    return { error: API_UNAVAILABLE };
  }

  // Токен истёк, пока обзорщик правил профиль. Введённое при этом пропадёт.
  if (result.response.status === 401) redirect("/login?returnTo=/settings");
  if (!result.response.ok || !result.data) {
    const message = problemMessage(result.error, "Не удалось сохранить профиль.");

    return { error: KNOWN_ERRORS[message] ?? message };
  }

  // Профиль и список обзорщиков на главной (имя, жанры, издание) показывают новое сразу.
  if (result.data.id) updateTag(reviewerTag(result.data.id));
  updateTag("reviewers");

  return { success: "Профиль сохранён." };
}
