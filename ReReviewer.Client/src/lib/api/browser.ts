import createClient from "openapi-fetch";
import type { paths } from "./schema";

// Для клиентских компонентов. Пути те же, что у API, но запросы идут на этот же сайт, в BFF-прокси
// (app/api/[...path]/route.ts). Он пропускает только чтение публичных данных из своего списка и без токена.
export const browserApi = createClient<paths>({ baseUrl: "" });
