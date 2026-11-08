import Link from "next/link";
import { Suspense } from "react";
import { LogoutButton } from "@/components/logout-button";
import { buttonVariants } from "@/components/ui/button";
import { isStaff } from "@/lib/auth/roles";
import { getCurrentUser } from "@/lib/auth/session";

export function SiteHeader() {
  return (
    <header className="border-b">
      {/* flex-wrap: на узком экране меню модератора не помещается рядом с названием и переносится на вторую строку. */}
      <div className="mx-auto flex min-h-14 w-full max-w-5xl flex-wrap items-center justify-between gap-x-4 gap-y-1 px-4 py-2">
        <Link href="/" className="font-semibold tracking-tight">
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
      <nav className="flex items-center gap-2">
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
      <span className="hidden text-sm text-muted-foreground sm:inline">{user.name}</span>
      <LogoutButton />
    </nav>
  );
}
