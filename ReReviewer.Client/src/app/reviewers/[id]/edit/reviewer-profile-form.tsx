"use client";

import { Plus, X } from "lucide-react";
import { startTransition, useActionState, useState, type FormEvent } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { NativeSelect, NativeSelectOption } from "@/components/ui/native-select";
import { Textarea } from "@/components/ui/textarea";
import type { GenreViewModel, OutletViewModel, ReviewerViewModel } from "@/lib/api/schema";
import { updateReviewerProfile } from "@/lib/profile/actions";

const MAX_LINKS = 5;
const MAX_ABOUT = 1000;

type Row = { key: string; value: string };
type OutletRow = { key: string; outletId: string; startedAt: string; endedAt: string };

const newKey = () =>
  crypto.randomUUID();

// Все поля управляемые, а отправка идёт через onSubmit, а не action формы: так React не сбрасывает поля
// после сохранения, и после ошибки всё введённое остаётся на месте.
export function ReviewerProfileForm({ reviewer, genres, outlets }: {
  reviewer: ReviewerViewModel;
  genres: GenreViewModel[];
  outlets: OutletViewModel[];
}) {
  const [state, formAction, pending] = useActionState(updateReviewerProfile, undefined);
  const [fullName, setFullName] = useState(reviewer.fullName);
  const [about, setAbout] = useState(reviewer.about ?? "");
  const [genreIds, setGenreIds] = useState(() => new Set(reviewer.genres.map((genre) => genre.id)));
  const [links, setLinks] = useState<Row[]>(() =>
    (reviewer.links.length > 0 ? reviewer.links : [""]).map((value) => ({ key: newKey(), value })));
  const [rows, setRows] = useState<OutletRow[]>(() => reviewer.outlets.map((outlet) => ({
    key: outlet.outletId,
    outletId: outlet.outletId,
    startedAt: outlet.startedAt ?? "",
    endedAt: outlet.endedAt ?? "",
  })));

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    startTransition(() => formAction(data));
  }

  const toggleGenre = (id: number | null) =>
    setGenreIds((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  const updateRow = (key: string, change: Partial<OutletRow>) =>
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...change } : row)));

  return (
    <form onSubmit={submit} className="flex flex-col gap-8 rounded-3xl border bg-card p-6">
      {state?.error && (
        <Alert variant="destructive">
          <AlertDescription>{state.error}</AlertDescription>
        </Alert>
      )}
      {state?.success && !pending && (
        <Alert>
          <AlertDescription>{state.success}</AlertDescription>
        </Alert>
      )}

      <div className="flex flex-col gap-2">
        <Label htmlFor="fullName">Имя или псевдоним</Label>
        <Input id="fullName" name="fullName" required maxLength={250} value={fullName} onChange={(e) => setFullName(e.target.value)} className="h-11" />
      </div>

      <fieldset className="flex flex-col gap-3">
        <legend className="text-sm font-medium">Жанры</legend>
        <p className="text-sm text-muted-foreground">О каких играх вы пишете или снимаете. Нужен хотя бы один.</p>
        <div className="flex flex-wrap gap-2">
          {genres.map((genre) => (
            <label key={genre.id}>
              <input
                type="checkbox"
                name="genre"
                value={genre.id ?? ""}
                checked={genreIds.has(genre.id)}
                onChange={() => toggleGenre(genre.id)}
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
        <Label htmlFor="about">О себе</Label>
        <Textarea
          id="about"
          name="about"
          rows={5}
          maxLength={MAX_ABOUT}
          value={about}
          onChange={(e) => setAbout(e.target.value)}
          placeholder="Чем вы занимаетесь, о каких играх рассказываете, чем ваши обзоры отличаются от других"
        />
        <p className="text-right text-xs text-muted-foreground tabular-nums">{about.length} / {MAX_ABOUT}</p>
      </div>

      <fieldset className="flex flex-col gap-3">
        <legend className="text-sm font-medium">Ссылки</legend>
        <p className="text-sm text-muted-foreground">Канал, сайт, соцсети — до {MAX_LINKS} адресов, начиная с https://.</p>
        {links.map((link, index) => (
          <div key={link.key} className="flex gap-2">
            <Input
              type="url"
              name="link"
              aria-label={`Ссылка ${index + 1}`}
              maxLength={300}
              placeholder="https://"
              value={link.value}
              onChange={(e) => setLinks((current) => current.map((row) => (row.key === link.key ? { ...row, value: e.target.value } : row)))}
              className="h-11"
            />
            <Button type="button" variant="ghost" size="icon" className="size-11 shrink-0" aria-label={`Убрать ссылку ${index + 1}`}
              onClick={() => setLinks((current) => current.filter((row) => row.key !== link.key))}>
              <X aria-hidden="true" />
            </Button>
          </div>
        ))}
        {links.length < MAX_LINKS && (
          <Button type="button" variant="outline" className="self-start" onClick={() => setLinks((current) => [...current, { key: newKey(), value: "" }])}>
            <Plus aria-hidden="true" />
            Добавить ссылку
          </Button>
        )}
      </fieldset>

      <fieldset className="flex flex-col gap-3">
        <legend className="text-sm font-medium">Места работы</legend>
        <p className="text-sm text-muted-foreground">Издания, где вы работаете или работали. Если работаете сейчас, оставьте дату окончания пустой.</p>
        {rows.map((row, index) => (
          <div key={row.key} className="flex flex-wrap items-end gap-3 rounded-2xl border p-4">
            <div className="flex min-w-48 flex-[2_1_14rem] flex-col gap-1.5">
              <Label htmlFor={`outlet-${row.key}`} className="text-sm">Издание</Label>
              <NativeSelect id={`outlet-${row.key}`} name="outletId" value={row.outletId} onChange={(e) => updateRow(row.key, { outletId: e.target.value })} className="w-full">
                <NativeSelectOption value="">Выберите издание</NativeSelectOption>
                {outlets.map((outlet) => (
                  <NativeSelectOption key={outlet.id} value={outlet.id ?? ""}
                    disabled={rows.some((other) => other.key !== row.key && other.outletId === outlet.id)}>
                    {outlet.name}
                  </NativeSelectOption>
                ))}
              </NativeSelect>
            </div>
            <div className="flex flex-[1_1_9rem] flex-col gap-1.5">
              <Label htmlFor={`start-${row.key}`} className="text-sm">С</Label>
              <Input id={`start-${row.key}`} type="date" name="outletStart" value={row.startedAt} onChange={(e) => updateRow(row.key, { startedAt: e.target.value })} className="h-9" />
            </div>
            <div className="flex flex-[1_1_9rem] flex-col gap-1.5">
              <Label htmlFor={`end-${row.key}`} className="text-sm">По</Label>
              <Input id={`end-${row.key}`} type="date" name="outletEnd" value={row.endedAt} min={row.startedAt || undefined} onChange={(e) => updateRow(row.key, { endedAt: e.target.value })} className="h-9" />
            </div>
            <Button type="button" variant="ghost" size="icon" className="size-9 shrink-0" aria-label={`Убрать место работы ${index + 1}`}
              onClick={() => setRows((current) => current.filter((other) => other.key !== row.key))}>
              <X aria-hidden="true" />
            </Button>
          </div>
        ))}
        <Button type="button" variant="outline" className="self-start"
          onClick={() => setRows((current) => [...current, { key: newKey(), outletId: "", startedAt: "", endedAt: "" }])}>
          <Plus aria-hidden="true" />
          Добавить место работы
        </Button>
      </fieldset>

      <Button type="submit" size="lg" disabled={pending} className="h-12 self-start px-5 text-base font-bold">
        {pending ? "Сохраняем…" : "Сохранить профиль"}
      </Button>
    </form>
  );
}
