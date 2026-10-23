using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Api.Middlewares;
using SharedKernel.Bl.Exceptions;

namespace SharedKernel.Api.Tests.Middlewares;

public class ErrorHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_CallsNextWhenNoExceptionIsThrown()
    {
        var context = CreateContext();
        var called = false;
        var sut = CreateSut();

        await sut.InvokeAsync(context, _ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        Assert.True(called);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_NotFoundException_ReturnsNotFoundProblem()
    {
        var (context, body) = await InvokeAsync(new NotFoundException("invoice was not found"));

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        AssertProblem(body, StatusCodes.Status404NotFound, "Entity not found");
        Assert.Contains("invoice was not found", body);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_ReturnsUnauthorizedProblem()
    {
        var (context, body) = await InvokeAsync(new UnauthorizedAccessException("secret"));

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        AssertProblem(body, StatusCodes.Status401Unauthorized, "Unauthorized");
        Assert.Contains("You do not have permission to perform this action.", body);
        Assert.DoesNotContain("secret", body);
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_ReturnsValidationProblem()
    {
        var (context, body) = await InvokeAsync(new ValidationException("invalid field"));

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        AssertProblem(body, StatusCodes.Status400BadRequest, "Validation failed");
        Assert.Contains("invalid field", body);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentNullExceptionWithFormat_ReturnsFormatProblem()
    {
        var (context, body) = await InvokeAsync(new ArgumentNullException("format"));

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        AssertProblem(body, StatusCodes.Status400BadRequest, "Validation format error");
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_ReturnsInvalidArgumentProblem()
    {
        var (context, body) = await InvokeAsync(new ArgumentException("bad argument"));

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        AssertProblem(body, StatusCodes.Status400BadRequest, "Invalid argument");
        Assert.Contains("bad argument", body);
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_ReturnsInvalidOperationProblem()
    {
        var (context, body) = await InvokeAsync(new InvalidOperationException("bad operation"));

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        AssertProblem(body, StatusCodes.Status400BadRequest, "Invalid operation");
        Assert.Contains("bad operation", body);
    }

    [Fact]
    public async Task InvokeAsync_UnexpectedException_ReturnsGenericInternalServerError()
    {
        var (context, body) = await InvokeAsync(new Exception("database password leaked"));

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        AssertProblem(body, StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        Assert.Contains("An internal server error has occurred. Please try again later.", body);
        Assert.DoesNotContain("database password leaked", body);
    }

    private static ErrorHandlingMiddleware CreateSut()
        => new(NullLogger<ErrorHandlingMiddleware>.Instance);

    private static async Task<(HttpContext Context, string Body)> InvokeAsync(Exception exception)
    {
        var context = CreateContext();
        var sut = CreateSut();

        await sut.InvokeAsync(context, _ => throw exception);

        context.Response.Body.Position = 0;

        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        return (context, body);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static void AssertProblem(string body, int statusCode, string title)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        Assert.Equal(statusCode, root.GetProperty("status").GetInt32());
        Assert.Equal(title, root.GetProperty("title").GetString());
    }
}
