import type { CurrentUser } from "./session";

// Названия ролей совпадают с RoleNames в API. Здесь они нужны только интерфейсу: права проверяет API.
export const ADMIN = "Admin";
export const MODERATOR = "Moderator";

export const isAdmin = (user: CurrentUser | null) =>
  !!user?.roles.includes(ADMIN);

// Модерация отзывов доступна и администраторам, и модераторам.
export const isStaff = (user: CurrentUser | null) =>
  !!user?.roles.some((role) => role === ADMIN || role === MODERATOR);
