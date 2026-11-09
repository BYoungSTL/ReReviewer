"use client";

import { useActionState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import type { GenreViewModel } from "@/lib/api/schema";
import { suggestReviewer } from "@/lib/requests/actions";

// canClaim = false, если аккаунт уже привязан к профилю: тогда отметки «это я» нет.
export function SuggestForm({ genres, canClaim }: { genres: GenreViewModel[]; canClaim: boolean }) {
  const [state, formAction, pending] = useActionState(suggestReviewer, undefined);
  const values = state?.values;

  return (
    <form action={formAction} className="flex flex-col gap-7 rounded-3xl border bg-card p-6">
      {state?.error && (
        <Alert variant="destructive">
          <AlertDescription>{state.error}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-2">
        <Label htmlFor="fullName">Имя или псевдоним</Label>
        <Input
          id="fullName"
          name="fullName"
          required
          maxLength={250}
          defaultValue={values?.fullName}
          placeholder="Например, Анна Смирнова или Иван «PixelHunter» Петров"
          className="h-11"
        />
      </div>

      <fieldset className="flex flex-col gap-3">
        <legend className="text-sm font-medium">Жанры</legend>
        <p className="text-sm text-muted-foreground">О каких играх пишет или снимает. Нужен хотя бы один.</p>
        <div className="flex flex-wrap gap-2">
          {genres.map((genre) => (
            <label key={genre.id}>
              <input
                type="checkbox"
                name="genre"
                value={genre.id ?? ""}
                defaultChecked={values?.genreIds.includes(String(genre.id))}
                className="peer sr-only"
              />
              <span className="inline-flex min-h-11 cursor-pointer items-center rounded-full border border-input px-4 font-semibold select-none hover:border-primary/60 peer-checked:border-primary peer-checked:bg-primary peer-checked:text-primary-foreground peer-focus-visible:ring-3 peer-focus-visible:ring-ring/50">
                {genre.title}
              </span>
            </label>
          ))}
        </div>
      </fieldset>

      <div className="flex flex-col gap-2">
        <Label htmlFor="note">Где публикуется</Label>
        <Textarea
          id="note"
          name="note"
          rows={4}
          maxLength={1000}
          defaultValue={values?.note}
          placeholder="Ссылки на канал, издание или соцсети: модератору так проще проверить"
        />
      </div>

      {canClaim && (
        <label className="flex cursor-pointer items-start gap-3 rounded-2xl border p-4 hover:border-primary/60">
          <input type="checkbox" name="isSelf" defaultChecked={values?.isSelf} className="mt-1 size-4 shrink-0 accent-primary" />
          <span className="flex flex-col gap-1">
            <span className="font-semibold">Это я</span>
            <span className="text-sm text-muted-foreground">
              После одобрения профиль привяжется к вашему аккаунту. Напишите в поле выше, как модератору убедиться,
              что это вы: например, дайте ссылку на пост, где вы упоминаете ReReviewer.
            </span>
          </span>
        </label>
      )}

      <div className="flex flex-col gap-2">
        <Button type="submit" size="lg" disabled={pending} className="h-12 self-start px-5 text-base font-bold">
          {pending ? "Отправляем…" : "Отправить на модерацию"}
        </Button>
        <p className="text-sm text-muted-foreground">Профиль появится на сайте после проверки модератором.</p>
      </div>
    </form>
  );
}
