import type { Metadata } from "next";
import Link from "next/link";
import { Suspense } from "react";
import { AuthCard } from "@/components/auth-card";
import { AuthForm } from "@/components/auth-form";
import { login } from "@/lib/auth/actions";

export const metadata: Metadata = {
  title: "Вход",
  robots: { index: false },
};

export default function LoginPage() {
  return (
    <AuthCard
      title="Вход"
      description="Войдите, чтобы писать отзывы об обзорщиках."
      footer={<span>Нет аккаунта? <Link href="/register" className="underline underline-offset-4">Зарегистрируйтесь</Link></span>}
    >
      {/* Форма читает ?returnTo из адреса, поэтому не входит в статическую оболочку. */}
      <Suspense>
        <AuthForm
          action={login}
          submitLabel="Войти"
          pendingLabel="Входим…"
          fields={[
            { name: "email", label: "Почта", type: "email", autoComplete: "email" },
            { name: "password", label: "Пароль", type: "password", autoComplete: "current-password" },
          ]}
        />
      </Suspense>
    </AuthCard>
  );
}
