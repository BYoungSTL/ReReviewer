import Link from "next/link";
import { Suspense } from "react";
import { Avatar } from "@/components/avatar";
import { LogoutButton } from "@/components/logout-button";
import { buttonVariants } from "@/components/ui/button";
import { isStaff } from "@/lib/auth/roles";
import { avatarUrl } from "@/lib/avatar";
import { getCurrentUser } from "@/lib/auth/session";

export function SiteHeader() {
  return (
    <header className="border-b">
      {/* flex-wrap: на узком экране меню модератора не помещается рядом с названием и переносится на вторую строку. */}
      <div className="mx-auto flex min-h-16 w-full max-w-6xl flex-wrap items-center justify-between gap-x-3 gap-y-1 px-4 py-2 sm:gap-x-4 sm:px-6">
        {/* На телефоне логотип меньше, чтобы меню гостя помещалось рядом с ним. */}
        <Link href="/" className="flex items-center gap-2 font-display text-base font-bold sm:gap-2.5 sm:text-lg">
          <span aria-hidden="true" className="flex size-7 items-center justify-center rounded-lg bg-primary text-sm text-primary-foreground sm:size-8">R</span>
          ReReviewer
        </Link>
        {/* Меню читает cookie, поэтому приходит отдельно от статической оболочки страницы. */}
        <Suspense fallback={<div className="h-7 w-40" />}>
          <UserMenu />
        </Suspense>
      </div>
    </header>
  );
}

async function UserMenu() {
  const user = await getCurrentUser();

  if (!user)
    return (
      <nav className="flex items-center gap-1 sm:gap-2">
        <Link href="/login" className={buttonVariants({ variant: "ghost", size: "sm" })}>
          Войти
        </Link>
        <Link href="/register" className={buttonVariants({ size: "sm" })}>
          Регистрация
        </Link>
      </nav>
    );

  return (
    <nav className="flex items-center gap-2">
      {isStaff(user) && (
        <Link href="/admin/reviews" className={buttonVariants({ variant: "ghost", size: "sm" })}>
          Модерация
        </Link>
      )}
      <Link href="/me" className={buttonVariants({ variant: "ghost", size: "sm" })}>
        Мои отзывы
      </Link>
      {/* Имя и аватарка берутся из токена: после изменений в настройках он перевыпускается, и шапка обновляется. */}
      <Link
        href="/settings"
        aria-label={`Настройки профиля: ${user.name}`}
        className="flex min-h-9 items-center gap-2 rounded-full pr-1 text-sm text-muted-foreground hover:text-foreground"
      >
        <Avatar id={user.id} name={user.name} src={avatarUrl(user.id, user.avatar)} size="xs" />
        <span className="hidden sm:inline">{user.name}</span>
      </Link>
      <LogoutButton />
    </nav>
  );
}
