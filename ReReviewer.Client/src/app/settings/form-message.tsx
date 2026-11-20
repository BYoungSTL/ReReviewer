import { Alert, AlertDescription } from "@/components/ui/alert";

// Ответ формы настроек: ошибка или подтверждение, что изменения сохранены.
export function FormMessage({ error, success }: { error?: string | null; success?: string | null }) {
  if (error)
    return (
      <Alert variant="destructive">
        <AlertDescription>{error}</AlertDescription>
      </Alert>
    );

  if (success)
    return (
      <Alert>
        <AlertDescription>{success}</AlertDescription>
      </Alert>
    );

  return null;
}
