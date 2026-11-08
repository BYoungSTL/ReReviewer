"use client";

import { useActionState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { NativeSelect, NativeSelectOption } from "@/components/ui/native-select";
import { Textarea } from "@/components/ui/textarea";
import type { CriterionViewModel, MaterialViewModel } from "@/lib/api/schema";
import { formatDate, materialFormatLabel } from "@/lib/format";
import type { ReviewFormState, ReviewFormValues } from "@/lib/reviews/actions";

const SCORES = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

// Форма нового отзыва и правки своего. Обязательны только оценка и текст:
// без критериев и материалов отзыв говорит о работе обзорщика в целом.
export function ReviewForm({ action, materials, criteria, initialValues, submitLabel, pendingLabel, hint }: {
  action: (state: ReviewFormState, form: FormData) => Promise<ReviewFormState>;
  materials: MaterialViewModel[];
  criteria: CriterionViewModel[];
  initialValues?: ReviewFormValues;
  submitLabel: string;
  pendingLabel: string;
  hint: string;
}) {
  const [state, formAction, pending] = useActionState(action, undefined);
  const values = state?.values ?? initialValues;

  return (
    <form action={formAction} className="flex flex-col gap-8">
      {state?.error && (
        <Alert variant="destructive">
          <AlertDescription>{state.error}</AlertDescription>
        </Alert>
      )}

      <fieldset className="flex flex-col gap-3">
        <legend className="text-sm font-medium">Общая оценка</legend>
        <div className="flex flex-wrap gap-1.5">
          {SCORES.map((score) => (
            <label key={score}>
              <input
                type="radio"
                name="score"
                value={score}
                required
                defaultChecked={values?.score === score}
                className="peer sr-only"
              />
              <span className="flex size-9 cursor-pointer items-center justify-center rounded-lg border text-sm tabular-nums transition-colors select-none hover:bg-muted peer-checked:border-primary peer-checked:bg-primary peer-checked:text-primary-foreground peer-focus-visible:ring-3 peer-focus-visible:ring-ring/50">
                {score}
              </span>
            </label>
          ))}
        </div>
        <p className="text-sm text-muted-foreground">1 — совсем не доверяю, 10 — лучший источник мнений об играх.</p>
      </fieldset>

      <div className="flex flex-col gap-2">
        <Label htmlFor="text">Отзыв</Label>
        <Textarea
          id="text"
          name="text"
          rows={8}
          required
          placeholder="Чем полезны или, наоборот, плохи материалы этого обзорщика?"
          defaultValue={values?.text}
        />
      </div>

      {criteria.length > 0 && (
        <fieldset className="flex flex-col gap-3">
          <legend className="text-sm font-medium">Оценки по критериям</legend>
          <p className="text-sm text-muted-foreground">Необязательно: оцените только то, о чём можете судить.</p>
          <div className="grid gap-x-8 gap-y-2 sm:grid-cols-2">
            {criteria.map((criterion) => {
              const score = values?.criterionScores[String(criterion.id)] ?? "";

              return <div key={criterion.id} className="flex items-center justify-between gap-4">
                <Label htmlFor={`criterion-${criterion.id}`} className="font-normal">{criterion.title}</Label>
                {/* У select React применяет defaultValue только при создании, поэтому key: иначе после ошибки оценка сбросится. */}
                <NativeSelect
                  key={score}
                  id={`criterion-${criterion.id}`}
                  name={`criterion-${criterion.id}`}
                  size="sm"
                  className="shrink-0"
                  defaultValue={score}
                >
                  <NativeSelectOption value="">—</NativeSelectOption>
                  {SCORES.map((score) => (
                    <NativeSelectOption key={score} value={score}>{score}</NativeSelectOption>
                  ))}
                </NativeSelect>
              </div>;
            })}
          </div>
        </fieldset>
      )}

      {materials.length > 0 && (
        <fieldset className="flex flex-col gap-3">
          <legend className="text-sm font-medium">О каких материалах отзыв</legend>
          <p className="text-sm text-muted-foreground">Необязательно: можно писать о работе обзорщика в целом.</p>
          <ul className="flex max-h-72 flex-col gap-1 overflow-y-auto">
            {materials.map((material) => (
              <li key={material.id}>
                <label className="flex cursor-pointer items-start gap-3 rounded-lg p-2 text-sm hover:bg-muted">
                  <input
                    type="checkbox"
                    name="material"
                    value={material.id ?? ""}
                    defaultChecked={!!material.id && values?.materialIds.includes(material.id)}
                    className="mt-0.5 size-4 shrink-0 accent-primary"
                  />
                  <span className="flex min-w-0 flex-col">
                    <span className="break-words">{material.title}</span>
                    <span className="text-muted-foreground">
                      {[
                        material.gameTitle,
                        materialFormatLabel(material.format),
                        material.outletName,
                        material.publishedAt && formatDate(material.publishedAt),
                      ].filter(Boolean).join(" · ")}
                    </span>
                  </span>
                </label>
              </li>
            ))}
          </ul>
        </fieldset>
      )}

      <div className="flex flex-col gap-2">
        <Button type="submit" size="lg" disabled={pending} className="self-start px-4">
          {pending ? pendingLabel : submitLabel}
        </Button>
        <p className="text-sm text-muted-foreground">{hint}</p>
      </div>
    </form>
  );
}
