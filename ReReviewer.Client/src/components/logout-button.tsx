"use client";

import { Button } from "@/components/ui/button";
import { logout } from "@/lib/auth/actions";

// После выхода страница загружается заново, а не переходом внутри приложения: Next.js хранит покинутые страницы
// скрытыми вместе с данными и черновиками (Activity), и следующий пользователь этого браузера нашёл бы их в DOM.
// Полная загрузка сбрасывает и их, и кэш TanStack Query. Cookie по-прежнему удаляет Server Action, защищённый от CSRF.
export function LogoutButton() {
  return (
    <form
      action={async () => {
        await logout();
        // eslint-disable-next-line @next/next/no-location-assign-relative-destination -- нужна именно полная загрузка, см. выше
        window.location.assign("/");
      }}
    >
      <Button type="submit" variant="outline" size="sm">
        Выйти
      </Button>
    </form>
  );
}
