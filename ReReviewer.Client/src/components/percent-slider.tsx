"use client";

import { cn } from "cn";
import { useState, type SyntheticEvent } from "react";
import { Label } from "@/components/ui/label";
import { formatPercent } from "@/lib/format";

// Оценка по критерию — процент от 0 до 100 ползунком. Пока ползунок не тронули, оценки нет: поле name не отправляется,
// а на месте числа — прочерк. Так критерий нельзя «оценить», просто пропустив его со значением по умолчанию.
// Значение управляемое, поэтому после ошибки отправки оно не теряется.
export function PercentSlider({ id, name, label, defaultValue }: {
  id: string;
  name: string;
  label: string;
  defaultValue: number | null;
}) {
  const [value, setValue] = useState<number | null>(defaultValue);
  // Клик по ползунку без сдвига тоже ставит оценку: иначе 50 % было бы не выбрать, не сдвинув ползунок туда и обратно.
  const pick = (event: SyntheticEvent<HTMLInputElement>) =>
    setValue(Number(event.currentTarget.value));

  return (
    <div className="flex flex-col gap-1.5">
      <div className="flex items-baseline justify-between gap-3">
        <Label htmlFor={id} className="font-normal">{label}</Label>
        <span className={cn("shrink-0 text-sm font-bold tabular-nums", value === null && "text-muted-foreground")}>
          {value === null ? "—" : formatPercent(value)}
        </span>
      </div>
      <input
        id={id}
        type="range"
        min={0}
        max={100}
        step={5}
        value={value ?? 50}
        onChange={pick}
        onClick={pick}
        aria-valuetext={value === null ? "Не оценено" : formatPercent(value)}
        className={cn("h-2 w-full cursor-pointer accent-primary", value === null && "opacity-40")}
      />
      {value !== null && <input type="hidden" name={name} value={value} />}
    </div>
  );
}
