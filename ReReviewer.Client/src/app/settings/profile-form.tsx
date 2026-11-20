"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { updateProfile } from "@/lib/settings/actions";
import { FormMessage } from "./form-message";

// После сохранения страница передаёт новые name и email, и React подставляет их в поля при сбросе формы.
export function ProfileForm({ name, email }: { name: string; email: string }) {
  const [state, formAction, pending] = useActionState(updateProfile, undefined);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <FormMessage error={state?.error} success={state?.success} />
      <div className="flex flex-col gap-2">
        <Label htmlFor="name">Имя</Label>
        <Input id="name" name="name" required maxLength={100} autoComplete="nickname" defaultValue={state?.values?.name ?? name} className="h-11" />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="email">Почта</Label>
        <Input id="email" name="email" type="email" required autoComplete="email" defaultValue={state?.values?.email ?? email} className="h-11" />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="profile-password">Текущий пароль</Label>
        <Input id="profile-password" name="currentPassword" type="password" autoComplete="current-password" className="h-11" />
        <p className="text-sm text-muted-foreground">Нужен, только если меняете почту: она служит логином.</p>
      </div>
      <Button type="submit" disabled={pending} className="self-start">{pending ? "Сохраняем…" : "Сохранить"}</Button>
    </form>
  );
}
