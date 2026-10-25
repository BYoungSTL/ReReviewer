using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace ReReviewer.Middleware;

// Переводит исключения сервисов в ответы ProblemDetails (application/problem+json):
// ValidationException → 400, AuthenticationException → 401, UnauthorizedAccessException → 403,
// KeyNotFoundException → 404, DbUpdateException → 409, всё остальное → 500.
// Подробности 409 и 500 отдаются только в Development, чтобы не раскрывать клиентам устройство базы.
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        // Если ответ уже начал отправляться, переписать его нельзя — исключение уходит дальше.
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, detail) = Describe(exception);

        if (status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogInformation("{Status} for {Method} {Path}: {Message}", status, context.Request.Method, context.Request.Path, exception.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }

    private (int Status, string Detail) Describe(Exception exception) =>
        exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, exception.Message),
            AuthenticationException => (StatusCodes.Status401Unauthorized, exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, exception.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            DbUpdateException => (StatusCodes.Status409Conflict, environment.IsDevelopment()
                ? exception.InnerException?.Message ?? exception.Message
                : "The change conflicts with existing data: a duplicate value or related records."),
            _ => (StatusCodes.Status500InternalServerError, environment.IsDevelopment()
                ? exception.ToString()
                : "An unexpected error occurred.")
        };
}