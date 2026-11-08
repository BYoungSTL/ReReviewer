# ReReviewer.Client

Сайт ReReviewer на Next.js 16 (App Router, Cache Components) + TypeScript, Tailwind CSS 4 и shadcn/ui (Base UI).

## Запуск

1. Запустите API (`ReReviewer.Server`, профиль `http` — http://localhost:5070).
2. В папке `ReReviewer.Client`:

   ```bash
   npm install
   npm run dev
   ```

3. Откройте http://localhost:3000.

Настройки — в `.env.local` (образец — `.env.example`): `API_URL` — адрес API, `SITE_URL` — публичный адрес сайта.

Весь проект целиком (база, миграции, API и сайт) поднимается из корня репозитория: `docker compose up --build -d`.

## Версия npm

Нужен npm 11.19.1 или новее: такой стоит в образе `node:24-alpine`. Старые версии (проверено на 11.6.2) при `npm install`
выбрасывают из `package-lock.json` необязательные пакеты `@emnapi/*`, и после этого сборка образа падает на `npm ci`
с ошибкой `Missing: @emnapi/runtime ... from lock file`. На старой версии каждая команда npm выводит предупреждение
`EBADDEVENGINES` (`devEngines` в `package.json`). Если файл уже испорчен, обновите npm и выполните
`npm install --package-lock-only`.

## Rider

Сайт входит в `ReReviewer.sln` как проект `ReReviewer.Client.esproj`, поэтому виден в дереве Solution вместе с остальными проектами
(`node_modules` и `.next` скрыты). Команду можно запустить прямо из `package.json` — значком слева от `dev`, `build` и других скриптов.

Проект нужен только для отображения. Сборка решения и `dotnet test` его не трогают, а его собственная сборка в Rider ничего не делает:
сайт собирается командами npm.

## Команды

| Команда | Что делает |
| --- | --- |
| `npm run dev` | сервер разработки |
| `npm run build` / `npm start` | production-сборка и её запуск |
| `npm run lint` | ESLint |
| `npm run typecheck` | проверка типов, включая типы маршрутов Next.js |
| `npm run gen:api` | заново сгенерировать типы API — после изменения контроллеров или ViewModel'ей |

## Типы API

`src/lib/api/schema.d.ts` генерируется из OpenAPI-документа API (`/openapi/v1.json`, только в Development) и хранится в git,
чтобы сайт собирался без запущенного API. Вручную его не правят: после изменений в API запустите API и `npm run gen:api`.

Все поля в типах обязательные (`--properties-required-by-default`): API всегда отдаёт все свойства, `null` — явно.
Обратная сторона — тела запросов тоже требуют всех полей. ViewModel'и API общие для чтения и записи,
и для форм записи (отзыв и т. п.) лучше завести в API отдельные модели запросов.

## Как устроено

- **Данные.** Server Components запрашивают API напрямую с сервера: `lib/api/client.ts` (openapi-fetch).
  Публичные данные (обзорщики, рейтинги, опубликованные отзывы) кэшируются на несколько минут — `lib/api/public.ts`.
- **Вход.** Server Actions (`lib/auth/actions.ts`) получают JWT от `POST /api/auth/login` и кладут его в httpOnly-cookie.
  JavaScript браузера токен не видит. Срок cookie равен сроку токена (`Jwt:LifetimeMinutes`, по умолчанию 60 минут);
  refresh-токенов в API пока нет, после истечения нужно войти снова. После выхода страница загружается заново:
  Next.js хранит покинутые страницы скрытыми вместе с данными и черновиками, и без перезагрузки они достались бы
  следующему пользователю этого браузера.
- **Запросы из браузера.** Клиентские компоненты обращаются к `/api/*` этого же сайта (`lib/api/browser.ts`),
  а BFF-прокси `app/api/[...path]/route.ts` пересылает запрос в API с токеном из cookie. CORS для этого не нужен.
- **Страницы для вошедших** перечислены в `matcher` в `src/proxy.ts`: без cookie там сразу редирект на вход.
- **SEO.** Страница обзорщика отдаёт заголовок, описание и Open Graph (`generateMetadata`). Поисковым ботам Next.js
  отдаёт страницу целиком, без потоковой загрузки. У несуществующего обзорщика ответ 200 со страницей «не найдено»
  и `noindex`: статус уже отправлен, когда выясняется, что обзорщика нет.

## Структура

```
src/
  app/                 маршруты: /, /reviewers/[id], /reviewers/[id]/review (новый отзыв), /reviews/[id]/edit (правка своего),
                       /login, /register, /me и BFF-прокси /api/*
  components/          общие компоненты; ui/ — компоненты shadcn (npx shadcn@latest add <имя>)
  lib/api/             клиенты API, кэшируемые запросы, разбор ошибок, сгенерированные типы
  lib/auth/            cookie с токеном, текущий пользователь, Server Actions входа
  lib/reviews/         Server Actions создания и правки отзыва, выбор критериев для формы
  proxy.ts             редирект гостей со страниц для вошедших
```
