"use client";

import { useSearchParams } from "next/navigation";
import { useActionState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { AuthFormState } from "@/lib/auth/actions";

type AuthField = {
  name: string;
  label: string;
  type: "text" | "email" | "password";
  autoComplete: string;
  minLength?: number;
};

// Форма входа или регистрации: Server Action, ошибка от API над полями и возврат на страницу из ?returnTo.
// Пароль после ошибки не восстанавливается — только остальные поля.
export function AuthForm({ action, fields, submitLabel, pendingLabel }: {
  action: (state: AuthFormState, form: FormData) => Promise<AuthFormState>;
  fields: AuthField[];
  submitLabel: string;
  pendingLabel: string;
}) {
  const returnTo = useSearchParams().get("returnTo") ?? "/";
  const [state, formAction, pending] = useActionState(action, undefined);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <input type="hidden" name="returnTo" value={returnTo} />
      {state?.error && (
        <Alert variant="destructive">
          <AlertDescription>{state.error}</AlertDescription>
        </Alert>
      )}
      {fields.map((field) => (
        <div key={field.name} className="flex flex-col gap-2">
          <Label htmlFor={field.name}>{field.label}</Label>
          <Input
            id={field.name}
            name={field.name}
            type={field.type}
            autoComplete={field.autoComplete}
            minLength={field.minLength}
            required
            defaultValue={field.type === "password" ? undefined : state?.values?.[field.name]}
          />
        </div>
      ))}
      <Button type="submit" disabled={pending} className="w-full">
        {pending ? pendingLabel : submitLabel}
      </Button>
    </form>
  );
}
