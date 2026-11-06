import Link from "next/link";
import { Suspense } from "react";
import { Button, buttonVariants } from "@/components/ui/button";
import { logout } from "@/lib/auth/actions";
import { getCurrentUser } from "@/lib/auth/session";

export function SiteHeader() {
  return (
    <header className="border-b">
      <div className="mx-auto flex h-14 w-full max-w-5xl items-center justify-between gap-4 px-4">
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
      <Link href="/me" className={buttonVariants({ variant: "ghost", size: "sm" })}>
        Мои отзывы
      </Link>
      <span className="hidden text-sm text-muted-foreground sm:inline">{user.name}</span>
      <form action={logout}>
        <Button type="submit" variant="outline" size="sm">
          Выйти
        </Button>
      </form>
    </nav>
  );
}
