"use server";

import { redirect } from "next/navigation";
import { userApi } from "@/lib/api/client";
import { API_UNAVAILABLE, problemMessage } from "@/lib/api/errors";
import { saveToken } from "@/lib/auth/session";

// Настройки своего профиля. Каждое изменение API подтверждает новым токеном: в нём имя, почта и версия аватарки,
// которые показывает шапка. Токен сохраняется в cookie, и Next.js сам перерисовывает страницу.

export type SettingsState = {
  error?: string;
  success?: string;
  values?: Record<string, string>;
} | undefined;

const MAX_AVATAR_BYTES = 512 * 1024;
const MIN_PASSWORD_LENGTH = 8;

const KNOWN_ERRORS: Record<string, string> = {
  "Current password is required to change email.": "Чтобы сменить почту, введите текущий пароль.",
  "Current password is required.": "Введите текущий пароль.",
  "Current password is incorrect.": "Неверный текущий пароль.",
  "Email is already in use.": "Эта почта уже занята.",
  "Email is not valid.": "Проверьте адрес почты.",
  "Email must not be empty.": "Укажите почту.",
  "User name must not be empty.": "Укажите имя.",
  [`Password must be at least ${MIN_PASSWORD_LENGTH} characters long.`]: `Пароль должен быть не короче ${MIN_PASSWORD_LENGTH} символов.`,
  "Avatar must be a PNG, JPEG or WebP image.": "Аватарка должна быть картинкой PNG, JPEG или WebP.",
  "Avatar must be at most 512 KB.": "Аватарка должна весить не больше 512 КБ.",
};

export async function updateProfile(_state: SettingsState, form: FormData): Promise<SettingsState> {
  const name = text(form, "name");
  const email = text(form, "email");
  const values = { name, email };

  const error = await send("Не удалось сохранить профиль.", async (api) =>
    api.PUT("/api/users/me", { body: { name, email, currentPassword: String(form.get("currentPassword") ?? "") || null } }));

  return error ? { error, values } : { success: "Профиль сохранён." };
}

export async function changePassword(_state: SettingsState, form: FormData): Promise<SettingsState> {
  const currentPassword = String(form.get("currentPassword") ?? "");
  const newPassword = String(form.get("newPassword") ?? "");

  if (newPassword !== String(form.get("repeatPassword") ?? "")) return { error: "Новые пароли не совпадают." };
  if (newPassword.length < MIN_PASSWORD_LENGTH) return { error: KNOWN_ERRORS[`Password must be at least ${MIN_PASSWORD_LENGTH} characters long.`] };

  const error = await send("Не удалось сменить пароль.", async (api) =>
    api.PUT("/api/users/me/password", { body: { currentPassword, newPassword } }));

  return error
    ? { error }
    : { success: "Пароль изменён. На других устройствах нужно будет войти заново." };
}

// Картинку уже уменьшила до 256×256 страница настроек, поэтому здесь только проверка размера.
export async function uploadAvatar(_state: SettingsState, form: FormData): Promise<SettingsState> {
  const file = form.get("avatar");
  if (!(file instanceof File) || file.size === 0) return { error: "Выберите картинку." };
  if (file.size > MAX_AVATAR_BYTES) return { error: KNOWN_ERRORS["Avatar must be at most 512 KB."] };

  const body = new FormData();
  body.append("file", file, file.name || "avatar");

  const error = await send("Не удалось сохранить аватарку.", async (api) =>
    // Тело — FormData: openapi-fetch отправит его как есть, с границей multipart.
    api.PUT("/api/users/me/avatar", { body: body as never }));

  return error ? { error } : { success: "Аватарка сохранена." };
}

export async function removeAvatar(): Promise<SettingsState> {
  const error = await send("Не удалось удалить аватарку.", async (api) => api.DELETE("/api/users/me/avatar"));

  return error ? { error } : { success: "Аватарка удалена." };
}

type TokenResult = {
  data?: { token: string; expiresAt: string };
  error?: unknown;
  response: Response;
};

// Возвращает текст ошибки или undefined при успехе. redirect — вне try: он работает через исключение.
async function send(fallback: string, request: (api: Awaited<ReturnType<typeof userApi>>) => Promise<TokenResult>) {
  let result: TokenResult;
  try {
    result = await request(await userApi());
  } catch {
    return API_UNAVAILABLE;
  }

  // Токен истёк или был выдан до смены пароля на другом устройстве.
  if (result.response.status === 401) redirect("/login?returnTo=/settings");
  if (!result.response.ok || !result.data) {
    const message = problemMessage(result.error, fallback);

    return KNOWN_ERRORS[message] ?? message;
  }

  await saveToken(result.data.token, result.data.expiresAt);
}

const text = (form: FormData, name: string) =>
  String(form.get(name) ?? "").trim();
