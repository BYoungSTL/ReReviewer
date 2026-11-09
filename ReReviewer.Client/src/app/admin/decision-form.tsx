"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import type { AdminActionState } from "@/lib/admin/actions";

export function ReviewModeration({ action }: {
  action: (state: AdminActionState, form: FormData) => Promise<AdminActionState>;
}) {
  const [state, formAction, pending] = useActionState(action, undefined);

  return (
    <form action={formAction} className="flex flex-col gap-2 border-t pt-3">
      {state?.error && <p className="text-sm text-destructive">{state.error}</p>}
      <div className="flex flex-wrap gap-2">
        <Button type="submit" name="decision" value="approve" size="sm" disabled={pending}>
          Одобрить
        </Button>
        <Button type="submit" name="decision" value="reject" variant="outline" size="sm" disabled={pending}>
          Отклонить
        </Button>
      </div>
    </form>
  );
}
