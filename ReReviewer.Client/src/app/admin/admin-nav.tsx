"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { buttonVariants } from "@/components/ui/button";

export function AdminNav({ showUsers }: { showUsers: boolean }) {
  const pathname = usePathname();
  const tabs = [
    { href: "/admin/reviews", label: "Отзывы" },
    { href: "/admin/reviewers", label: "Новые обзорщики" },
    { href: "/admin/claims", label: "Привязка профилей" },
    ...(showUsers ? [{ href: "/admin/users", label: "Пользователи" }] : []),
  ] as const;

  return (
    <nav className="flex flex-wrap gap-1 border-b pb-2">
      {tabs.map((tab) => {
        const active = pathname.startsWith(tab.href);

        return (
          <Link
            key={tab.href}
            href={tab.href}
            aria-current={active ? "page" : undefined}
            className={buttonVariants({ variant: active ? "secondary" : "ghost", size: "sm" })}
          >
            {tab.label}
          </Link>
        );
      })}
    </nav>
  );
}
