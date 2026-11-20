"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { NativeSelect, NativeSelectOption } from "@/components/ui/native-select";
import { Textarea } from "@/components/ui/textarea";
import { claimSelectedReviewer } from "@/lib/requests/actions";
import { FormMessage } from "./form-message";

// Запрос «это я» из настроек: профиль выбирают в списке ещё не привязанных. После отправки — «Мои отзывы» со статусом.
export function ClaimSelectForm({ reviewers }: { reviewers: { id: string; name: string }[] }) {
  const [state, formAction, pending] = useActionState(claimSelectedReviewer, undefined);
  const reviewerId = state?.values.reviewerId ?? "";

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <FormMessage error={state?.error} />
      <div className="flex flex-col gap-2">
        <Label htmlFor="claim-reviewer">Ваш профиль на сайте</Label>
        {/* У select React применяет defaultValue только при создании, поэтому key: иначе после ошибки выбор сбросится. */}
        <NativeSelect key={reviewerId} id="claim-reviewer" name="reviewerId" required defaultValue={reviewerId} className="w-full">
          <NativeSelectOption value="">Выберите обзорщика</NativeSelectOption>
          {reviewers.map((reviewer) => (
            <NativeSelectOption key={reviewer.id} value={reviewer.id}>{reviewer.name}</NativeSelectOption>
          ))}
        </NativeSelect>
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="claim-note">Как модератору убедиться, что это вы</Label>
        <Textarea
          id="claim-note"
          name="note"
          rows={4}
          required
          maxLength={1000}
          defaultValue={state?.values.note}
          placeholder="Например, ссылка на пост или видео, где вы называете свой аккаунт на ReReviewer"
        />
      </div>
      <Button type="submit" disabled={pending} className="self-start">{pending ? "Отправляем…" : "Отправить запрос"}</Button>
    </form>
  );
}
