import type { NextRequest } from "next/server";
import { API_URL } from "@/lib/api/client";
import { getToken } from "@/lib/auth/session";

// BFF-прокси для клиентских компонентов (lib/api/browser.ts): /api/* этого сайта → /api/* ReReviewer API
// с токеном из httpOnly-cookie. Токен остаётся на сервере, а браузеру не нужен CORS — запросы идут на тот же сайт.
// Вход и регистрация закрыты: их ответ с токеном не должен попасть в браузер (см. lib/auth/actions.ts).
const BLOCKED = new Set(["auth/login", "auth/register"]);
const PASSED_HEADERS = ["content-type", "cache-control", "x-content-type-options", "content-security-policy"];

async function forward(request: NextRequest, { params }: RouteContext<"/api/[...path]">) {
  const { path } = await params;
  const unsafe = path.some((segment) => segment === "." || segment === ".." || segment.includes("/"));
  if (unsafe || BLOCKED.has(path.join("/").toLowerCase()))
    return Response.json({ title: "Not Found", status: 404 }, { status: 404 });

  const headers = new Headers({ accept: "application/json" });
  const contentType = request.headers.get("content-type");
  if (contentType) headers.set("content-type", contentType);

  const token = await getToken();
  if (token) headers.set("authorization", `Bearer ${token}`);

  const target = new URL(`/api/${path.map(encodeURIComponent).join("/")}${request.nextUrl.search}`, API_URL);
  const hasBody = request.method !== "GET" && request.method !== "HEAD";

  let response: Response;
  try {
    response = await fetch(target, {
      method: request.method,
      headers,
      body: hasBody ? await request.arrayBuffer() : undefined,
    });
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

export { forward as GET, forward as POST, forward as PUT, forward as DELETE };
