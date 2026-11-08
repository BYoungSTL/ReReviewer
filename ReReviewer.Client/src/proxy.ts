import { NextResponse, type NextRequest } from "next/server";
import { TOKEN_COOKIE } from "@/lib/auth/cookie";

// Быстрая проверка до рендера: без cookie с токеном страницы для вошедших сразу уводят на вход.
// Это только удобство, не защита: просроченный токен она пропустит, на него ответит 401 API, и страница уведёт на вход.
export function proxy(request: NextRequest) {
  if (request.cookies.has(TOKEN_COOKIE)) return NextResponse.next();

  const login = new URL("/login", request.url);
  login.searchParams.set("returnTo", request.nextUrl.pathname + request.nextUrl.search);

  return NextResponse.redirect(login);
}

export const config = {
  matcher: ["/me/:path*", "/reviewers/:id/review", "/reviews/:id/edit"],
};
