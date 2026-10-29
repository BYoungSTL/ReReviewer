using System.ComponentModel.DataAnnotations;
using System.Security.Authentication;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using ReReviewer.Middleware;

namespace ReReviewer.UnitTests.Server;

public class ExceptionHandlingMiddlewareTests
{
    private const string DatabaseDetails = "Cannot insert duplicate key row with unique index 'IX_Users_Email'.";

    [Theory]
    [InlineData(nameof(ValidationException), 400)]
    [InlineData(nameof(AuthenticationException), 401)]
    [InlineData(nameof(UnauthorizedAccessException), 403)]
    [InlineData(nameof(KeyNotFoundException), 404)]
    [InlineData(nameof(DbUpdateException), 409)]
    [InlineData(nameof(InvalidOperationException), 500)]
    public async Task Invoke_MapsExceptionToProblemDetails(string exceptionType, int expectedStatus)
    {
        var context = await InvokeAsync(CreateException(exceptionType), Environments.Production);
        var problem = ReadProblem(context);

        context.Response.StatusCode.ShouldBe(expectedStatus);
        context.Response.ContentType.ShouldStartWith("application/problem+json");
        problem.GetProperty("status").GetInt32().ShouldBe(expectedStatus);
        problem.GetProperty("instance").GetString().ShouldBe("/api/test");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Invoke_ServiceMessage_IsSentAsDetail()
    {
        var context = await InvokeAsync(new KeyNotFoundException("Review not found."), Environments.Production);

        ReadProblem(context).GetProperty("detail").GetString().ShouldBe("Review not found.");
    }

    [Fact]
    public async Task Invoke_ConflictInProduction_HidesDatabaseDetails()
    {
        var context = await InvokeAsync(CreateException(nameof(DbUpdateException)), Environments.Production);

        ReadProblem(context).GetProperty("detail").GetString()!.ShouldNotContain("IX_Users_Email");
    }

    [Fact]
    public async Task Invoke_ConflictInDevelopment_ShowsDatabaseDetails()
    {
        var context = await InvokeAsync(CreateException(nameof(DbUpdateException)), Environments.Development);

        ReadProblem(context).GetProperty("detail").GetString().ShouldBe(DatabaseDetails);
    }

    [Fact]
    public async Task Invoke_ServerErrorInProduction_HidesMessage()
    {
        var context = await InvokeAsync(new InvalidOperationException("secret internals"), Environments.Production);

        ReadProblem(context).GetProperty("detail").GetString().ShouldBe("An unexpected error occurred.");
    }

    [Fact]
    public async Task Invoke_NoException_LeavesResponseUntouched()
    {
        var context = await InvokeAsync(null, Environments.Production);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status200OK);
        context.Response.Body.Length.ShouldBe(0);
    }

    private static Exception CreateException(string type) =>
        type switch
        {
            nameof(ValidationException) => new ValidationException("Score must be between 1 and 10."),
            nameof(AuthenticationException) => new AuthenticationException("User is not authenticated."),
            nameof(UnauthorizedAccessException) => new UnauthorizedAccessException("Only the author can edit this review."),
            nameof(KeyNotFoundException) => new KeyNotFoundException("Review not found."),
            nameof(DbUpdateException) => new DbUpdateException("save failed", new Exception(DatabaseDetails)),
            _ => new InvalidOperationException("boom")
        };

    private static async Task<HttpContext> InvokeAsync(Exception? exception, string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);

        var middleware = new ExceptionHandlingMiddleware(
            _ => exception is null ? Task.CompletedTask : throw exception,
            NullLogger<ExceptionHandlingMiddleware>.Instance,
            environment);

        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        context.Request.Path = "/api/test";
        await middleware.InvokeAsync(context);

        return context;
    }

    private static JsonElement ReadProblem(HttpContext context)
    {
        context.Response.Body.Position = 0;

        return JsonDocument.Parse(context.Response.Body).RootElement;
    }
}