"use server";

import { redirect } from "next/navigation";
import { publicApi } from "@/lib/api/client";
import { API_UNAVAILABLE, problemMessage } from "@/lib/api/errors";
import { clearToken, saveToken } from "./session";

// Вход и регистрация — только здесь, на сервере: ответ API с токеном не должен попасть в браузер.
// Server Actions сами защищены от CSRF: Next.js сверяет заголовок Origin с адресом сайта.

export type AuthFormState = {
  error?: string;
  values?: Record<string, string>;
} | undefined;

export async function login(_state: AuthFormState, form: FormData): Promise<AuthFormState> {
  const email = field(form, "email");

  const error = await signIn(email, field(form, "password"));
  if (error) return { error, values: { email } };

  redirect(returnPath(form));
}

export async function register(_state: AuthFormState, form: FormData): Promise<AuthFormState> {
  const name = field(form, "name");
  const email = field(form, "email");
  const password = field(form, "password");

  try {
    const { error } = await publicApi.POST("/api/auth/register", { body: { name, email, password } });
    if (error) return { error: problemMessage(error, "Не удалось зарегистрироваться."), values: { name, email } };
  } catch {
    return { error: API_UNAVAILABLE, values: { name, email } };
  }

  // Регистрация токен не выдаёт, поэтому сразу входим.
  const error = await signIn(email, password);
  if (error) return { error, values: { name, email } };

  redirect(returnPath(form));
}

// Без redirect: после выхода страницу полностью загружает заново LogoutButton (components/logout-button.tsx).
export async function logout() {
  await clearToken();
}

async function signIn(email: string, password: string): Promise<string | undefined> {
  try {
    const { data, error, response } = await publicApi.POST("/api/auth/login", { body: { email, password } });
    if (response.status === 401) return "Неверная почта или пароль.";
    if (!data) return problemMessage(error, "Не удалось войти.");

    await saveToken(data.token, data.expiresAt);
  } catch {
    return API_UNAVAILABLE;
  }
}

const field = (form: FormData, name: string) =>
  String(form.get(name) ?? "").trim();

// Только путь этого сайта: иначе ссылка /login?returnTo=https://evil.example уводила бы после входа на чужой сайт.
function returnPath(form: FormData) {
  const path = field(form, "returnTo");

  return path.startsWith("/") && !path.startsWith("//") && !path.startsWith("/\\") ? path : "/";
}
