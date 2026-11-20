"use client";

import { Button } from "@/components/ui/button";

// Ошибка при рендере страницы — чаще всего API недоступен. Подробности — в логе сервера Next.js по error.digest.
export default function Error({ retry }: { error: unknown; retry: () => void }) {
  return (
    <div className="flex flex-col items-start gap-4">
      <h1 className="text-3xl font-semibold tracking-tight">Не удалось загрузить страницу</h1>
      <p className="text-muted-foreground">Сервер ReReviewer не ответил. Попробуйте ещё раз через минуту.</p>
      <Button variant="outline" onClick={() => retry()}>
        Попробовать снова
      </Button>
    </div>
  );
}
