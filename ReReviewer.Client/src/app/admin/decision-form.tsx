"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import type { AdminActionState } from "@/lib/admin/actions";

// Решение модератора по отзыву, заявке или номинациям: нажатая кнопка передаёт decision = approve или reject.
export function DecisionForm({ action, approveLabel = "Одобрить", rejectLabel = "Отклонить" }: {
  action: (state: AdminActionState, form: FormData) => Promise<AdminActionState>;
  approveLabel?: string;
  rejectLabel?: string;
}) {
  const [state, formAction, pending] = useActionState(action, undefined);

  return (
    <form action={formAction} className="flex flex-col gap-2 border-t pt-3">
      {state?.error && <p className="text-sm text-destructive">{state.error}</p>}
      <div className="flex flex-wrap gap-2">
        <Button type="submit" name="decision" value="approve" size="sm" disabled={pending}>
          {approveLabel}
        </Button>
        <Button type="submit" name="decision" value="reject" variant="outline" size="sm" disabled={pending}>
          {rejectLabel}
        </Button>
      </div>
    </form>
  );
}
