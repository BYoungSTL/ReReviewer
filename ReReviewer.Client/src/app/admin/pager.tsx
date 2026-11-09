import Link from "next/link";
import { buttonVariants } from "@/components/ui/button";

// Переход между страницами очереди. После решений страница может опустеть, поэтому «Назад» ведёт не дальше последней.
export function Pager({ basePath, page, totalPages }: { basePath: string; page: number; totalPages: number }) {
  if (totalPages <= 1 && page <= 1) return null;

  const link = buttonVariants({ variant: "outline", size: "sm" });

  return (
    <nav className="flex items-center gap-3 self-center">
      {page > 1 && <Link href={`${basePath}?page=${Math.min(page - 1, Math.max(totalPages, 1))}`} className={link}>Назад</Link>}
      <span className="text-sm text-muted-foreground">Страница {page} из {Math.max(totalPages, 1)}</span>
      {page < totalPages && <Link href={`${basePath}?page=${page + 1}`} className={link}>Дальше</Link>}
    </nav>
  );
}

// Номер страницы из адреса: всё нечисловое и меньше единицы — первая страница.
export const pageParam = (value: string | string[] | undefined) =>
  Math.max(1, Math.trunc(Number(value)) || 1);
