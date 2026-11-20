"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { changePassword } from "@/lib/settings/actions";
import { FormMessage } from "./form-message";

// Поля паролей после отправки всегда очищаются: React сбрасывает форму, а значения обратно не возвращаются.
export function PasswordForm() {
  const [state, formAction, pending] = useActionState(changePassword, undefined);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <FormMessage error={state?.error} success={state?.success} />
      <div className="flex flex-col gap-2">
        <Label htmlFor="current-password">Текущий пароль</Label>
        <Input id="current-password" name="currentPassword" type="password" required autoComplete="current-password" className="h-11" />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="new-password">Новый пароль</Label>
        <Input id="new-password" name="newPassword" type="password" required minLength={8} autoComplete="new-password" className="h-11" />
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="repeat-password">Новый пароль ещё раз</Label>
        <Input id="repeat-password" name="repeatPassword" type="password" required minLength={8} autoComplete="new-password" className="h-11" />
      </div>
      <p className="text-sm text-muted-foreground">Не короче 8 символов. После смены на других устройствах нужно будет войти заново.</p>
      <Button type="submit" disabled={pending} className="self-start">{pending ? "Меняем…" : "Сменить пароль"}</Button>
    </form>
  );
}
