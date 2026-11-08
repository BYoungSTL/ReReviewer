import type { Metadata } from "next";
import { Suspense } from "react";
import { isAdmin } from "@/lib/auth/roles";
import { getCurrentUser } from "@/lib/auth/session";
import { AdminNav } from "./admin-nav";

export const metadata: Metadata = {
  title: { template: "%s — модерация ReReviewer", default: "Модерация" },
  robots: { index: false },
};

// Вкладки показываются по ролям из токена, но это только интерфейс: права проверяет API,
// и страница без нужной роли получает 403 и показывает «нет доступа».
export default function AdminLayout({ children }: LayoutProps<"/admin">) {
  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-3">
        <h1 className="text-3xl font-semibold tracking-tight">Модерация</h1>
        <Suspense fallback={<div className="h-8" />}>
          <Nav />
        </Suspense>
      </div>
      {children}
    </div>
  );
}

async function Nav() {
  const user = await getCurrentUser();

  return <AdminNav showUsers={isAdmin(user)} />;
}
