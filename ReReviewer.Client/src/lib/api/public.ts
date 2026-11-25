import "server-only";
import { cacheLife, cacheTag } from "next/cache";
import { failed, publicApi } from "./client";

// Публичные данные, одинаковые для всех посетителей. Хранятся в кэше Next.js несколько минут (профиль "minutes"),
// раньше их сбрасывает revalidateTag/updateTag с тегом. Ответ 404 кэшируется как null, ошибки не кэшируются.

export const REVIEWS_PAGE_SIZE = 10;

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

// Id в адресе страницы проверяется до запроса к API, чтобы случайные адреса не заполняли кэш.
export const isGuid = (value: string) =>
  GUID.test(value);

export const reviewerTag = (id: string) =>
  `reviewer:${id}`;

// Публичная страница пользователя: имя, аватарка, привязанный профиль обзорщика и опубликованные отзывы.
export const userTag = (id: string) =>
  `user:${id}`;

// Отзывы и рейтинги всех обзорщиков: например, удаление пользователя убирает его отзывы сразу у нескольких обзорщиков.
export const REVIEWS_TAG = "reviews";

export async function getReviewers() {
  "use cache";
  cacheLife("minutes");
  cacheTag("reviewers");

  const { data, response } = await publicApi.GET("/api/reviewers");

  return data ?? failed(response);
}

export async function getReviewer(id: string) {
  "use cache";
  cacheLife("minutes");
  cacheTag(reviewerTag(id));

  const { data, response } = await publicApi.GET("/api/reviewers/{id}", { params: { path: { id } } });
  if (response.status === 404) return null;

  return data ?? failed(response);
}

export async function getReviewerRating(id: string) {
  "use cache";
  cacheLife("minutes");
  cacheTag(reviewerTag(id), REVIEWS_TAG);

  const { data, response } = await publicApi.GET("/api/reviewers/{id}/rating", { params: { path: { id } } });
  if (response.status === 404) return null;

  return data ?? failed(response);
}

export async function getReviewerMaterials(id: string) {
  "use cache";
  cacheLife("minutes");
  cacheTag(reviewerTag(id));

  const { data, response } = await publicApi.GET("/api/reviewers/{id}/materials", { params: { path: { id } } });

  return data ?? failed(response);
}

export async function getCriteria() {
  "use cache";
  cacheLife("minutes");
  cacheTag("criteria");

  const { data, response } = await publicApi.GET("/api/criteria");

  return data ?? failed(response);
}

export async function getOutlets() {
  "use cache";
  cacheLife("minutes");
  cacheTag("outlets");

  const { data, response } = await publicApi.GET("/api/outlets");

  return data ?? failed(response);
}

// Знаки, на которые можно номинировать обзорщика. Их набор меняют только через API, поэтому кэш живёт свои минуты.
export async function getBadges() {
  "use cache";
  cacheLife("minutes");
  cacheTag("badges");

  const { data, response } = await publicApi.GET("/api/badges");

  return data ?? failed(response);
}

// Игры, к которым можно предложить материал обзорщика. Каталог меняют только через API.
export async function getGames() {
  "use cache";
  cacheLife("minutes");
  cacheTag("games");

  const { data, response } = await publicApi.GET("/api/games");

  return data ?? failed(response);
}

// Игры для выпадающих списков форм: по алфавиту, только с Id.
export async function getGameOptions() {
  const games = await getGames();

  return games
    .flatMap((game) => (game.id === null ? [] : [{ id: game.id, title: game.title }]))
    .sort((a, b) => a.title.localeCompare(b.title, "ru"));
}

export async function getGenres() {
  "use cache";
  cacheLife("minutes");
  cacheTag("genres");

  const { data, response } = await publicApi.GET("/api/genres");

  return data ?? failed(response);
}

// Без почты и ролей: только то, что видно всем.
export async function getPublicUser(id: string) {
  "use cache";
  cacheLife("minutes");
  cacheTag(userTag(id));

  const { data, response } = await publicApi.GET("/api/users/{id}/profile", { params: { path: { id } } });
  if (response.status === 404) return null;

  return data ?? failed(response);
}

// Опубликованные отзывы пользователя, новые сверху.
export async function getUserReviews(id: string, page = 1) {
  "use cache";
  cacheLife("minutes");
  cacheTag(userTag(id), REVIEWS_TAG);

  const { data, response } = await publicApi.GET("/api/users/{id}/reviews", {
    params: { path: { id }, query: { page, pageSize: REVIEWS_PAGE_SIZE } },
  });

  return data ?? failed(response);
}

// Опубликованные отзывы, новые сверху.
export async function getReviewerReviews(id: string, page = 1) {
  "use cache";
  cacheLife("minutes");
  cacheTag(reviewerTag(id), REVIEWS_TAG);

  const { data, response } = await publicApi.GET("/api/reviewers/{id}/reviews", {
    params: { path: { id }, query: { page, pageSize: REVIEWS_PAGE_SIZE } },
  });

  return data ?? failed(response);
}
