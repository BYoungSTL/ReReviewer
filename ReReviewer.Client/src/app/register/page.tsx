import type { Metadata } from "next";
import Link from "next/link";
import { Suspense } from "react";
import { AuthCard } from "@/components/auth-card";
import { AuthForm } from "@/components/auth-form";
import { register } from "@/lib/auth/actions";

export const metadata: Metadata = {
  title: "Регистрация",
  robots: { index: false },
};

export default function RegisterPage() {
  return (
    <AuthCard
      title="Регистрация"
      description="Аккаунт нужен, чтобы оценивать обзорщиков."
      footer={<span>Уже есть аккаунт? <Link href="/login" className="underline underline-offset-4">Войдите</Link></span>}
    >
      <Suspense>
        <AuthForm
          action={register}
          submitLabel="Зарегистрироваться"
          pendingLabel="Создаём аккаунт…"
          fields={[
            { name: "name", label: "Имя", type: "text", autoComplete: "nickname" },
            { name: "email", label: "Почта", type: "email", autoComplete: "email" },
            // Как в UserAppService: пароль не короче 8 символов.
            { name: "password", label: "Пароль", type: "password", autoComplete: "new-password", minLength: 8 },
          ]}
        />
      </Suspense>
    </AuthCard>
  );
}
