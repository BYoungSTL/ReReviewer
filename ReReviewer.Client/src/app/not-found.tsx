import Link from "next/link";
import { buttonVariants } from "@/components/ui/button";

export default function NotFound() {
  return (
    <div className="flex flex-col items-start gap-4">
      <h1 className="text-3xl font-semibold tracking-tight">Страница не найдена</h1>
      <p className="text-muted-foreground">Возможно, обзорщика удалили или в ссылке опечатка.</p>
      <Link href="/" className={buttonVariants({ variant: "outline" })}>
        На главную
      </Link>
    </div>
  );
}
