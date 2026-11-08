import "server-only";
import { cookies } from "next/headers";
import { cache } from "react";
import { TOKEN_COOKIE } from "./cookie";

// JWT из POST /api/auth/login хранится в httpOnly-cookie: JavaScript браузера его не видит,
// поэтому XSS не может его украсть. SameSite=Lax — cookie не уходит с чужих сайтов в POST-запросах (защита от CSRF),
// но приходит при переходе по ссылке, и пользователь, открывший сайт из поиска, остаётся вошедшим.

export type CurrentUser = {
  id: string;
  name: string;
  email: string;
  roles: string[];
};

export async function getToken() {
  return (await cookies()).get(TOKEN_COOKIE)?.value;
}

// Менять cookie можно только в Server Action или Route Handler, не при рендере страницы.
export async function saveToken(token: string, expiresAt: string) {
  (await cookies()).set(TOKEN_COOKIE, token, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/",
    expires: new Date(expiresAt),
  });
}

export async function clearToken() {
  (await cookies()).delete(TOKEN_COOKIE);
}

// Пользователь из claims токена (sub, name, email, role — см. JwtTokenService). Подпись здесь не проверяется:
// ключа у фронтенда нет и не должно быть. Результат нужен только интерфейсу — права проверяет API на каждом запросе.
// Срок токена тоже не проверяется: браузер удаляет cookie вместе с его истечением (saveToken), а на просроченный
// токен API ответит 401. Date.now() здесь и нельзя вызывать: с Cache Components он запрещён при пререндере.
// cache — чтобы несколько компонентов в одном запросе не разбирали токен заново.
export const getCurrentUser = cache(async (): Promise<CurrentUser | null> => {
  const token = await getToken();
  if (!token) return null;

  try {
    const claims = JSON.parse(Buffer.from(token.split(".")[1], "base64url").toString("utf8"));

    return {
      id: claims.sub,
      name: claims.name,
      email: claims.email,
      // Одна роль приходит строкой, несколько — массивом.
      roles: [claims.role ?? []].flat(),
    };
  } catch {
    return null;
  }
});
