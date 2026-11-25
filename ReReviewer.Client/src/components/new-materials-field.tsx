"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { NativeSelect, NativeSelectOption } from "@/components/ui/native-select";
import { materialFormatLabel } from "@/lib/format";
import { MATERIAL_FORMATS } from "@/lib/materials/constants";
import { EMPTY_NEW_MATERIAL, MAX_NEW_MATERIALS, NEW_MATERIAL_FIELD, type NewMaterialValues } from "@/lib/reviews/new-materials";

type Row = { key: number; values: NewMaterialValues };

// Материалы, которых у обзорщика ещё нет: автор добавляет их прямо в отзыв, модератор проверяет их вместе с отзывом.
// Поля не управляемые: значения строк берутся из initial при создании. Поэтому после ошибки форма отзыва создаёт
// этот блок заново (key от присланных значений) — иначе сброс формы после Server Action вернул бы пустые поля.
export function NewMaterialsField({ games, initial }: {
  games: { id: string; title: string }[];
  initial: NewMaterialValues[];
}) {
  const [rows, setRows] = useState<Row[]>(() => initial.map((values, key) => ({ key, values })));
  const [nextKey, setNextKey] = useState(initial.length);

  const add = () => {
    setRows((current) => [...current, { key: nextKey, values: EMPTY_NEW_MATERIAL }]);
    setNextKey((key) => key + 1);
  };
  const remove = (key: number) =>
    setRows((current) => current.filter((row) => row.key !== key));

  return (
    <div className="flex flex-col gap-3">
      {rows.map(({ key, values }, index) => (
        <div key={key} className="flex flex-col gap-4 rounded-2xl border p-4">
          <div className="flex items-center justify-between gap-3">
            <span className="text-sm font-medium">Новый материал {rows.length > 1 && index + 1}</span>
            <Button type="button" variant="ghost" size="sm" onClick={() => remove(key)}>Убрать</Button>
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor={`new-material-title-${key}`}>Название</Label>
            <Input
              id={`new-material-title-${key}`}
              name={NEW_MATERIAL_FIELD.title}
              required
              maxLength={250}
              defaultValue={values.title}
              placeholder="Как называется статья или видео"
            />
          </div>
          <div className="flex flex-col gap-2">
            <Label htmlFor={`new-material-url-${key}`}>Ссылка</Label>
            <Input
              id={`new-material-url-${key}`}
              name={NEW_MATERIAL_FIELD.url}
              type="url"
              required
              maxLength={800}
              defaultValue={values.url}
              placeholder="https://"
            />
          </div>
          <div className="grid gap-4 sm:grid-cols-3">
            <div className="flex flex-col gap-2">
              <Label htmlFor={`new-material-game-${key}`}>Игра</Label>
              <NativeSelect id={`new-material-game-${key}`} name={NEW_MATERIAL_FIELD.gameId} required defaultValue={values.gameId} className="w-full">
                <NativeSelectOption value="">Выберите игру</NativeSelectOption>
                {games.map((game) => (
                  <NativeSelectOption key={game.id} value={game.id}>{game.title}</NativeSelectOption>
                ))}
              </NativeSelect>
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor={`new-material-format-${key}`}>Формат</Label>
              <NativeSelect id={`new-material-format-${key}`} name={NEW_MATERIAL_FIELD.format} required defaultValue={values.format} className="w-full">
                {MATERIAL_FORMATS.map((format) => (
                  <NativeSelectOption key={format} value={format}>{materialFormatLabel(format)}</NativeSelectOption>
                ))}
              </NativeSelect>
            </div>
            <div className="flex flex-col gap-2">
              <Label htmlFor={`new-material-date-${key}`}>Дата публикации</Label>
              <Input id={`new-material-date-${key}`} name={NEW_MATERIAL_FIELD.publishedAt} type="date" defaultValue={values.publishedAt} />
            </div>
          </div>
        </div>
      ))}

      {rows.length < MAX_NEW_MATERIALS && (
        <Button type="button" variant="outline" size="sm" onClick={add} className="self-start">
          {rows.length === 0 ? "Добавить новый материал" : "Добавить ещё материал"}
        </Button>
      )}
    </div>
  );
}
