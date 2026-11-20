"use client";

import { cn } from "cn";
import { startTransition, useActionState, useState, type ChangeEvent } from "react";
import { Avatar } from "@/components/avatar";
import { Button, buttonVariants } from "@/components/ui/button";
import { removeAvatar, uploadAvatar } from "@/lib/settings/actions";
import { FormMessage } from "./form-message";

const SIZE = 256;

type Picked = { blob: Blob; preview: string };

// Выбранная картинка обрезается до квадрата по центру и уменьшается до 256×256 прямо в браузере:
// на сервер уходит несколько десятков килобайт, а API проверяет, что это действительно картинка.
// После сохранения страница передаёт новый src, и key пересоздаёт компонент — превью и состояние сбрасываются.
export function AvatarSettings({ userId, name, src }: { userId: string; name: string; src: string | null }) {
  const [uploadState, upload, uploading] = useActionState(uploadAvatar, undefined);
  const [removeState, remove, removing] = useActionState(removeAvatar, undefined);
  const [picked, setPicked] = useState<Picked | null>(null);
  const [pickError, setPickError] = useState<string | null>(null);

  async function pick(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;

    try {
      setPicked(await squareImage(file));
      setPickError(null);
    } catch {
      setPickError("Не удалось прочитать картинку. Подойдёт PNG, JPEG или WebP.");
    }
  }

  function save() {
    if (!picked) return;

    const form = new FormData();
    form.append("avatar", picked.blob, picked.blob.type === "image/webp" ? "avatar.webp" : "avatar.png");
    startTransition(() => upload(form));
  }

  return (
    <div className="flex flex-col gap-4">
      <FormMessage error={pickError ?? uploadState?.error ?? removeState?.error} />
      <div className="flex flex-wrap items-center gap-5">
        {picked
          // eslint-disable-next-line @next/next/no-img-element -- превью из data: URL, оптимизатору нечего делать
          ? <img src={picked.preview} alt="Новая аватарка" className="size-20 rounded-full object-cover sm:size-22" />
          : <Avatar id={userId} name={name} src={src} size="lg" />}
        <div className="flex flex-wrap gap-2">
          <label className={cn(buttonVariants({ variant: "outline" }), "cursor-pointer focus-within:ring-3 focus-within:ring-ring/50")}>
            {picked || src ? "Выбрать другую" : "Выбрать картинку"}
            <input type="file" accept="image/png,image/jpeg,image/webp" className="sr-only" onChange={pick} />
          </label>
          {picked && (
            <>
              <Button type="button" onClick={save} disabled={uploading}>{uploading ? "Сохраняем…" : "Сохранить"}</Button>
              <Button type="button" variant="ghost" onClick={() => setPicked(null)} disabled={uploading}>Отмена</Button>
            </>
          )}
          {!picked && src && (
            <form action={remove}>
              <Button type="submit" variant="ghost" disabled={removing}>{removing ? "Удаляем…" : "Удалить"}</Button>
            </form>
          )}
        </div>
      </div>
      <p className="text-sm text-muted-foreground">PNG, JPEG или WebP. Картинка обрежется до квадрата по центру.</p>
    </div>
  );
}

async function squareImage(file: File): Promise<Picked> {
  const bitmap = await createImageBitmap(file);
  const side = Math.min(bitmap.width, bitmap.height);

  const canvas = document.createElement("canvas");
  canvas.width = SIZE;
  canvas.height = SIZE;
  const context = canvas.getContext("2d");
  if (!context) throw new Error("Canvas is not available.");

  context.drawImage(bitmap, (bitmap.width - side) / 2, (bitmap.height - side) / 2, side, side, 0, 0, SIZE, SIZE);
  bitmap.close();

  // Браузер, который не умеет кодировать WebP, вернёт PNG — его API тоже принимает.
  const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, "image/webp", 0.9));
  if (!blob) throw new Error("Image encoding failed.");

  return { blob, preview: canvas.toDataURL(blob.type, 0.9) };
}
