// Адрес аватарки через BFF-прокси этого сайта. Версия (время загрузки) меняет адрес при каждой новой картинке,
// поэтому браузер может кэшировать её надолго. Без версии аватарки нет — null.
export const avatarUrl = (userId: string, version: string | null | undefined) =>
  version ? `/api/users/${userId}/avatar?v=${encodeURIComponent(version)}` : null;
