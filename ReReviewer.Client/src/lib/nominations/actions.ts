"use server";

import { refresh } from "next/cache";
import { redirect } from "next/navigation";
import { userApi } from "@/lib/api/client";
import { API_UNAVAILABLE, problemMessage } from "@/lib/api/errors";

// Номинация обзорщика на знак со страницы обзорщика. Одна форма на все знаки: нажатая кнопка передаёт
// nominate=<Id знака> (номинировать) или withdraw=<Id знака> (отозвать свою номинацию). Число номинаций видят
// только модераторы, поэтому публичный кэш страницы не сбрасывается — только перерисовывается состояние кнопок.

export type NominationState = { error: string } | undefined;

const KNOWN_ERRORS: Record<string, string> = {
  "You cannot nominate your own reviewer profile.": "Свой профиль номинировать нельзя.",
  "This reviewer already has this badge.": "У обзорщика уже есть этот знак.",
  "You have already nominated this reviewer for this badge.": "Вы уже номинировали обзорщика на этот знак.",
};

export async function toggleNomination(reviewerId: string, _state: NominationState, form: FormData): Promise<NominationState> {
  const intent = form.has("nominate") ? "nominate" : form.has("withdraw") ? "withdraw" : null;
  const badgeId = intent ? Number(form.get(intent)) : Number.NaN;
  if (!intent || !Number.isInteger(badgeId) || badgeId <= 0) return { error: "Не удалось понять, какой знак выбран." };

  let result: { error?: unknown; response: Response };
  try {
    const api = await userApi();
    result = intent === "nominate"
      ? await api.POST("/api/nominations", { body: { reviewerId, badgeId } })
      : await api.DELETE("/api/nominations/{reviewerId}/{badgeId}", { params: { path: { reviewerId, badgeId } } });
  } catch {
    return { error: API_UNAVAILABLE };
  }

  // Токен истёк, пока страница была открыта.
  if (result.response.status === 401) redirect(`/login?returnTo=${encodeURIComponent(`/reviewers/${reviewerId}`)}`);

  // Кнопки показывают состояние после ответа, в том числе после ошибки: например, знак могли только что выдать.
  refresh();

  if (!result.response.ok) {
    const message = problemMessage(result.error, "Не удалось сохранить номинацию.");

    return { error: KNOWN_ERRORS[message] ?? message };
  }
}
