import type { NextRequest } from "next/server";
import { API_URL } from "@/lib/api/client";
import { isGuid } from "@/lib/api/public";

// BFF-прокси для браузера: /api/* этого сайта → /api/* ReReviewer API, без CORS — запросы идут на тот же сайт.
// Пропускает только чтение публичных данных, которые браузер загружает сам: следующие страницы отзывов
// (components/reviews-list.tsx) и аватарки (lib/avatar.ts). Токен из cookie прокси не добавляет.
// Всё, что меняет данные или читает данные вошедшего, идёт через Server Actions и серверные компоненты.
// Иначе XSS мог бы действовать от имени пользователя, а то и унести токен: PUT api/users/me и другие правки
// своего профиля отвечают новым токеном.
const PASSED_HEADERS = ["content-type", "cache-control", "x-content-type-options", "content-security-policy"];

// reviewers/{id}/reviews, users/{id}/reviews и users/{id}/avatar.
const ALLOWED: Record<string, string[]> = {
  reviewers: ["reviews"],
  users: ["reviews", "avatar"],
};

export async function GET(request: NextRequest, { params }: RouteContext<"/api/[...path]">) {
  const { path } = await params;
  const [resource, id, action, ...rest] = path;
  if (rest.length > 0 || !isGuid(id ?? "") || !Object.hasOwn(ALLOWED, resource) || !ALLOWED[resource].includes(action))
    return Response.json({ title: "Not Found", status: 404 }, { status: 404 });

  // Сегменты проверены выше: в них только буквы, цифры и дефисы, кодировать нечего.
  const target = new URL(`/api/${resource}/${id}/${action}${request.nextUrl.search}`, API_URL);

  let response: Response;
  try {
    response = await fetch(target, { headers: { accept: "application/json" } });
  } catch {
    return Response.json({ title: "Bad Gateway", status: 502, detail: "ReReviewer API is unavailable." }, { status: 502 });
  }

  // Наружу уходят только заголовки из списка: Location, Server и прочие заголовки API браузеру не нужны.
  // Кэширование и защитные заголовки — для аватарок, которые браузер загружает отсюда как картинки.
  const responseHeaders = new Headers();
  for (const name of PASSED_HEADERS) {
    const value = response.headers.get(name);
    if (value) responseHeaders.set(name, value);
  }

  return new Response(response.body, { status: response.status, headers: responseHeaders });
}
