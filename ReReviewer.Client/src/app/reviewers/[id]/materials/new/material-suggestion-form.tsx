"use client";

import { useActionState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { NativeSelect, NativeSelectOption } from "@/components/ui/native-select";
import { materialFormatLabel } from "@/lib/format";
import { suggestMaterial } from "@/lib/materials/actions";
import { MATERIAL_FORMATS } from "@/lib/materials/constants";

// После ошибки поля восстанавливаются из state.values, после успеха форма очищается — можно предложить ещё один материал.
// У select React применяет defaultValue только при создании, поэтому у них key: иначе после ошибки выбор сбросится.
export function MaterialSuggestionForm({ reviewerId, games }: {
  reviewerId: string;
  games: { id: string; title: string }[];
}) {
  const [state, formAction, pending] = useActionState(suggestMaterial.bind(null, reviewerId), undefined);
  const values = state?.values;

  if (games.length === 0)
    return <p className="rounded-2xl border bg-card p-5">Пока в каталоге нет игр, к которым можно привязать материал.</p>;

  return (
    <form action={formAction} className="flex flex-col gap-6 rounded-3xl border bg-card p-6">
      {state?.error && (
        <Alert variant="destructive">
          <AlertDescription>{state.error}</AlertDescription>
        </Alert>
      )}
      {state?.success && (
        <Alert>
          <AlertDescription>{state.success}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-2">
        <Label htmlFor="title">Название</Label>
        <Input id="title" name="title" required maxLength={250} defaultValue={values?.title} placeholder="Как называется статья или видео" className="h-11" />
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="url">Ссылка</Label>
        <Input id="url" name="url" type="url" required maxLength={800} defaultValue={values?.url} placeholder="https://" className="h-11" />
      </div>

      <div className="grid gap-6 sm:grid-cols-2">
        <div className="flex flex-col gap-2">
          <Label htmlFor="gameId">Игра</Label>
          <NativeSelect key={`game-${values?.gameId ?? ""}`} id="gameId" name="gameId" required defaultValue={values?.gameId ?? ""} className="w-full">
            <NativeSelectOption value="">Выберите игру</NativeSelectOption>
            {games.map((game) => (
              <NativeSelectOption key={game.id} value={game.id}>{game.title}</NativeSelectOption>
            ))}
          </NativeSelect>
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="format">Формат</Label>
          <NativeSelect key={`format-${values?.format ?? ""}`} id="format" name="format" required defaultValue={values?.format ?? "Video"} className="w-full">
            {MATERIAL_FORMATS.map((format) => (
              <NativeSelectOption key={format} value={format}>{materialFormatLabel(format)}</NativeSelectOption>
            ))}
          </NativeSelect>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="publishedAt">Дата публикации</Label>
        <Input id="publishedAt" name="publishedAt" type="date" defaultValue={values?.publishedAt} className="h-11 sm:max-w-56" />
        <p className="text-sm text-muted-foreground">Необязательно.</p>
      </div>

      <Button type="submit" disabled={pending} className="self-start">{pending ? "Отправляем…" : "Отправить на проверку"}</Button>
    </form>
  );
}
