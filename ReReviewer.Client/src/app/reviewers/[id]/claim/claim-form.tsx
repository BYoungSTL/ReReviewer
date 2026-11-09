"use client";

import { useActionState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import type { ClaimFormState } from "@/lib/requests/actions";

export function ClaimForm({ action }: { action: (state: ClaimFormState, form: FormData) => Promise<ClaimFormState> }) {
  const [state, formAction, pending] = useActionState(action, undefined);

  return (
    <form action={formAction} className="flex flex-col gap-6 rounded-3xl border bg-card p-6">
      {state?.error && (
        <Alert variant="destructive">
          <AlertDescription>{state.error}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-2">
        <Label htmlFor="note">Как модератору убедиться, что это вы</Label>
        <Textarea
          id="note"
          name="note"
          rows={5}
          required
          maxLength={1000}
          defaultValue={state?.values.note}
          placeholder="Например, ссылка на пост или видео, где вы упоминаете свой аккаунт на ReReviewer"
        />
        <p className="text-sm text-muted-foreground">
          Подойдёт ссылка на публикацию в вашем канале или соцсети, где вы называете имя своего аккаунта здесь,
          или рабочая почта издания, по которой с вами можно связаться.
        </p>
      </div>

      <div className="flex flex-col gap-2">
        <Button type="submit" size="lg" disabled={pending} className="h-12 self-start px-5 text-base font-bold">
          {pending ? "Отправляем…" : "Отправить запрос"}
        </Button>
        <p className="text-sm text-muted-foreground">После одобрения профиль привяжется к вашему аккаунту.</p>
      </div>
    </form>
  );
}
