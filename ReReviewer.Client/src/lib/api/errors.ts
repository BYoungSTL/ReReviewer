// Ошибки API приходят как ProblemDetails (ExceptionHandlingMiddleware), а при невалидном теле запроса —
// как ValidationProblemDetails с полем errors ([ApiController]). Сообщения в них на английском.
type Problem = {
  title?: string | null;
  detail?: string | null;
  errors?: Record<string, string[]>;
};

export function problemMessage(error: unknown, fallback: string): string {
  if (!error || typeof error !== "object") return fallback;

  const problem = error as Problem;
  const firstError = problem.errors ? Object.values(problem.errors).flat()[0] : undefined;

  return problem.detail || firstError || problem.title || fallback;
}
