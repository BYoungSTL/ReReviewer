import type { Metadata } from "next";
import Form from "next/form";
import Link from "next/link";
import { redirect } from "next/navigation";
import { Suspense } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { moderateUser } from "@/lib/admin/actions";
import { failed, userApi } from "@/lib/api/client";
import type { RoleViewModel, UserViewModel } from "@/lib/api/schema";
import { getCurrentUser } from "@/lib/auth/session";
import { formatDate, roleLabel } from "@/lib/format";
import { NoAccess } from "../no-access";
import { UserModeration } from "./user-moderation";

export const metadata: Metadata = {
  title: "Пользователи",
};

export default function UsersPage({ searchParams }: PageProps<"/admin/users">) {
  return (
    <Suspense fallback={<p className="text-muted-foreground">Загружаем пользователей…</p>}>
      <Users searchParams={searchParams} />
    </Suspense>
  );
}

// API отдаёт всех пользователей одним списком, поэтому поиск — здесь, по имени и почте.
async function Users({ searchParams }: Pick<PageProps<"/admin/users">, "searchParams">) {
  const { q } = await searchParams;
  const query = typeof q === "string" ? q.trim() : "";

  const api = await userApi();
  const [users, roles, currentUser] = await Promise.all([api.GET("/api/users"), api.GET("/api/roles"), getCurrentUser()]);
  // proxy.ts уже увёл гостей без cookie, здесь отсеиваются просроченные токены и заблокированные аккаунты.
  if (users.response.status === 401) redirect("/login?returnTo=/admin/users");
  if (users.response.status === 403) return <NoAccess role="администраторам" />;
  const allUsers = users.data ?? failed(users.response);
  const allRoles = roles.data ?? failed(roles.response);

  const needle = query.toLocaleLowerCase("ru");
  const found = needle
    ? allUsers.filter((user) => `${user.name} ${user.email}`.toLocaleLowerCase("ru").includes(needle))
    : allUsers;
  const blockedCount = allUsers.filter((user) => user.blockedAt).length;

  return (
    <div className="flex flex-col gap-4">
      <Form action="/admin/users" className="flex gap-2">
        <Input key={query} name="q" defaultValue={query} placeholder="Имя или почта" aria-label="Поиск пользователей" className="max-w-xs" />
        <Button type="submit" variant="outline">Найти</Button>
      </Form>

      <p className="text-sm text-muted-foreground">
        Всего {allUsers.length}, заблокировано {blockedCount}{query && <>, найдено {found.length}</>}.
        Новые роли начинают действовать, когда пользователь войдёт заново. Блокировка действует сразу,
        а отзывы заблокированного остаются: их можно отклонить через модерацию.
      </p>

      {found.length === 0 && <p className="text-muted-foreground">Никого не нашлось.</p>}
      {found.map((user) => (
        <UserCard key={user.id} user={user} roles={allRoles} isSelf={user.id === currentUser?.id} />
      ))}
    </div>
  );
}

function UserCard({ user, roles, isSelf }: { user: UserViewModel; roles: RoleViewModel[]; isSelf: boolean }) {
  const roleIds = user.roles.flatMap((role) => (role.id === null ? [] : [role.id]));

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex flex-wrap items-baseline gap-x-3">
          <span>{user.name}</span>
          {isSelf && <span className="text-sm font-normal text-muted-foreground">это вы</span>}
          {user.blockedAt && (
            <span className="text-sm font-normal text-destructive">заблокирован {formatDate(user.blockedAt)}</span>
          )}
        </CardTitle>
        <CardDescription className="break-all">
          {user.email} · с {formatDate(user.createdAt)}
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <p className="flex flex-wrap gap-x-4 gap-y-1">
          <span>{user.roles.length > 0 ? user.roles.map((role) => roleLabel(role.title)).join(", ") : "Без ролей"}</span>
          {user.reviewerId && (
            <Link href={`/reviewers/${user.reviewerId}`} className="underline underline-offset-4">
              Профиль обзорщика
            </Link>
          )}
        </p>
        {/* Себя менять здесь нельзя: можно лишиться доступа к разделу. API и сам не даёт заблокировать себя.
            key пересоздаёт форму, когда роли или блокировка изменились: иначе в скрытой странице (Activity) остались бы старые отметки. */}
        {!isSelf && user.id && (
          <UserModeration
            key={`${roleIds.join(",")}|${user.blockedAt ?? ""}`}
            action={moderateUser.bind(null, user.id)}
            roles={roles}
            userRoleIds={roleIds}
            blocked={!!user.blockedAt}
          />
        )}
      </CardContent>
    </Card>
  );
}
