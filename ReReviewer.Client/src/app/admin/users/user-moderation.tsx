"use client";

import { useActionState, useLayoutEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import type { AdminActionState } from "@/lib/admin/actions";
import type { RoleViewModel } from "@/lib/api/schema";
import { roleLabel } from "@/lib/format";

// Роли, блокировка и удаление одного пользователя. Удаление необратимо и стирает отзывы, поэтому требует второго нажатия.
export function UserModeration({ action, roles, userRoleIds, blocked }: {
  action: (state: AdminActionState, form: FormData) => Promise<AdminActionState>;
  roles: RoleViewModel[];
  userRoleIds: number[];
  blocked: boolean;
}) {
  const [state, formAction, pending] = useActionState(action, undefined);
  const [confirmDelete, setConfirmDelete] = useState(false);

  // Next.js хранит покинутую страницу скрытой (Activity): без сброса при возврате кнопка удаления была бы уже взведена.
  useLayoutEffect(() => () => setConfirmDelete(false), []);

  return (
    <form action={formAction} className="flex flex-col gap-3 border-t pt-3">
      {state?.error && <p className="text-sm text-destructive">{state.error}</p>}

      <fieldset className="flex flex-wrap items-center gap-x-4 gap-y-2">
        <legend className="sr-only">Роли</legend>
        {roles.map((role) => (
          <label key={role.id} className="flex cursor-pointer items-center gap-2 text-sm">
            <input
              type="checkbox"
              name="role"
              value={role.id ?? ""}
              defaultChecked={role.id !== null && userRoleIds.includes(role.id)}
              className="size-4 accent-primary"
            />
            {roleLabel(role.title)}
          </label>
        ))}
        <Button type="submit" name="intent" value="roles" variant="outline" size="sm" disabled={pending}>
          Сохранить роли
        </Button>
      </fieldset>

      <div className="flex flex-wrap gap-2">
        <Button
          type="submit"
          name="intent"
          value={blocked ? "unblock" : "block"}
          variant={blocked ? "outline" : "secondary"}
          size="sm"
          disabled={pending}
        >
          {blocked ? "Разблокировать" : "Заблокировать"}
        </Button>
        {confirmDelete ? (
          <>
            <Button type="submit" name="intent" value="delete" variant="destructive" size="sm" disabled={pending}>
              Удалить вместе с отзывами
            </Button>
            <Button type="button" variant="ghost" size="sm" onClick={() => setConfirmDelete(false)}>
              Отмена
            </Button>
          </>
        ) : (
          <Button type="button" variant="ghost" size="sm" onClick={() => setConfirmDelete(true)}>
            Удалить…
          </Button>
        )}
      </div>
    </form>
  );
}
