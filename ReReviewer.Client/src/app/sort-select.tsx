"use client";

import { NativeSelect, NativeSelectOption } from "@/components/ui/native-select";

// Сортировка применяется сразу при выборе: форма поиска отправляется сама. Без JavaScript — кнопкой «Найти».
export function SortSelect({ value, options }: { value: string; options: Record<string, string> }) {
  return (
    <NativeSelect
      key={value}
      id="sort"
      name="sort"
      defaultValue={value}
      onChange={(event) => event.currentTarget.form?.requestSubmit()}
    >
      {Object.entries(options).map(([key, label]) => (
        <NativeSelectOption key={key} value={key}>{label}</NativeSelectOption>
      ))}
    </NativeSelect>
  );
}
