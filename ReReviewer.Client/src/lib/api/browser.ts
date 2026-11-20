import createClient from "openapi-fetch";
import type { paths } from "./schema";

// Для клиентских компонентов. Пути те же, что у API, но запросы идут на этот же сайт, в BFF-прокси
// (app/api/[...path]/route.ts), который добавляет токен из httpOnly-cookie. Сам токен браузеру недоступен.
export const browserApi = createClient<paths>({ baseUrl: "" });
