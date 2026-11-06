import "server-only";
import createClient from "openapi-fetch";
import { getToken } from "@/lib/auth/session";
import type { paths } from "./schema";

// Адрес API знает только сервер Next.js: браузер обращается к API через /api/* (app/api/[...path]/route.ts).
export const API_URL = process.env.API_URL ?? "http://localhost:5070";

// Без токена — для публичных данных, одинаковых для всех посетителей. Их можно кэшировать (lib/api/public.ts).
export const publicApi = createClient<paths>({ baseUrl: API_URL });

// С токеном текущего пользователя. Токен лежит в cookie, поэтому клиент доступен только во время запроса.
export async function userApi() {
  const token = await getToken();

  return createClient<paths>({
    baseUrl: API_URL,
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });
}

export function failed(response: Response): never {
  throw new Error(`ReReviewer API responded ${response.status} for ${response.url}`);
}
