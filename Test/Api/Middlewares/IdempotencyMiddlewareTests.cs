using Microsoft.Extensions.Caching.Memory;
using SharedKernel.Api.Middlewares;

namespace SharedKernel.Api.Tests.Middlewares;

public class IdempotencyMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithoutHeader_CallsNext()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new IdempotencyMiddleware(cache);
        var context = CreateContext();
        var calls = 0;

        await sut.InvokeAsync(context, _ =>
        {
            calls++;
            return context.Response.WriteAsync("payload");
        });

        Assert.Equal(1, calls);
        Assert.Equal("payload", ReadBody(context));
    }

    [Fact]
    public async Task InvokeAsync_WithSameKey_ReturnsCachedResponseOnSecondCall()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new IdempotencyMiddleware(cache);
        var calls = 0;

        var first = CreateContext("same-key");
        await sut.InvokeAsync(first, _ =>
        {
            calls++;
            return first.Response.WriteAsync("first-response");
        });

        var second = CreateContext("same-key");
        await sut.InvokeAsync(second, _ =>
        {
            calls++;
            return second.Response.WriteAsync("second-response");
        });

        Assert.Equal(1, calls);
        Assert.Equal("first-response", ReadBody(first));
        Assert.Equal("first-response", ReadBody(second));
        Assert.Equal("application/json", second.Response.ContentType);
    }

    [Fact]
    public async Task InvokeAsync_WithDifferentKeys_CallsNextForEachRequest()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new IdempotencyMiddleware(cache);
        var calls = 0;

        var first = CreateContext("key-one");
        await sut.InvokeAsync(first, _ =>
        {
            calls++;
            return first.Response.WriteAsync("one");
        });

        var second = CreateContext("key-two");
        await sut.InvokeAsync(second, _ =>
        {
            calls++;
            return second.Response.WriteAsync("two");
        });

        Assert.Equal(2, calls);
        Assert.Equal("one", ReadBody(first));
        Assert.Equal("two", ReadBody(second));
    }

    [Fact]
    public async Task InvokeAsync_EmptyResponse_IsCachedAsEmptyString()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new IdempotencyMiddleware(cache);
        var calls = 0;

        var first = CreateContext("empty-key");
        await sut.InvokeAsync(first, _ =>
        {
            calls++;
            return Task.CompletedTask;
        });

        var second = CreateContext("empty-key");
        await sut.InvokeAsync(second, _ =>
        {
            calls++;
            return Task.CompletedTask;
        });

        Assert.Equal(1, calls);
        Assert.Equal("", ReadBody(first));
        Assert.Equal("", ReadBody(second));
    }

    private static DefaultHttpContext CreateContext(string? idempotencyKey = null)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        if (idempotencyKey is not null)
            context.Request.Headers["Idempotency-Key"] = idempotencyKey;

        return context;
    }

    private static string ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;

        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return reader.ReadToEnd();
    }
}
