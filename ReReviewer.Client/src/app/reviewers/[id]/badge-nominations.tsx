"use client";

import { cn } from "cn";
import { Award, Check } from "lucide-react";
import { useActionState } from "react";
import { buttonVariants } from "@/components/ui/button";
import { toggleNomination } from "@/lib/nominations/actions";

// Номинация обзорщика на знаки, которых у него ещё нет. Кнопка знака номинирует, повторное нажатие — отзывает
// свою номинацию. После ответа страница перерисовывается (refresh в Server Action) и приносит новое состояние.
export function BadgeNominations({ reviewerId, badges, nominatedIds }: {
  reviewerId: string;
  badges: { id: number; title: string }[];
  nominatedIds: number[];
}) {
  const [state, formAction, pending] = useActionState(toggleNomination.bind(null, reviewerId), undefined);

  return (
    <form action={formAction} className="flex flex-col gap-3">
      {state?.error && <p className="text-sm text-destructive">{state.error}</p>}
      <ul className="flex flex-wrap gap-2">
        {badges.map((badge) => {
          const nominated = nominatedIds.includes(badge.id);

          return (
            <li key={badge.id}>
              <button
                type="submit"
                name={nominated ? "withdraw" : "nominate"}
                value={badge.id}
                disabled={pending}
                aria-pressed={nominated}
                title={nominated ? "Вы номинировали обзорщика. Нажмите, чтобы отозвать номинацию." : "Номинировать на этот знак"}
                className={cn(
                  buttonVariants({ variant: "outline", size: "sm" }),
                  "h-9 gap-1.5 rounded-full px-3",
                  nominated && "border-score-mid bg-score-mid/15 text-score-mid hover:bg-score-mid/25 hover:text-score-mid",
                )}
              >
                {nominated ? <Check aria-hidden="true" className="size-3.5" /> : <Award aria-hidden="true" className="size-3.5" />}
                {badge.title}
              </button>
            </li>
          );
        })}
      </ul>
    </form>
  );
}
