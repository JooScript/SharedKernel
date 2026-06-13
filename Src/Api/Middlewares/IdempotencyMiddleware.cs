using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace SharedKernel.Api.Middlewares;

/// <summary>
/// Middleware responsible for handling idempotent HTTP requests
/// using an in-memory cache.
/// </summary>
/// <remarks>
/// This middleware prevents duplicate request processing by caching
/// responses based on the value of the <c>Idempotency-Key</c> request header.
/// If the same key is received again within the cache duration,
/// the cached response is returned instead of executing the request again.
/// </remarks>
/// <param name="cache">
/// The in-memory cache used to store request responses.
/// </param>
public class IdempotencyMiddleware(IMemoryCache cache) : IMiddleware
{
    /// <summary>
    /// Processes the current HTTP request and applies idempotency behavior.
    /// </summary>
    /// <param name="context">
    /// The current HTTP context.
    /// </param>
    /// <param name="_next">
    /// The next middleware delegate in the request pipeline.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous middleware operation.
    /// </returns>
    public async Task InvokeAsync(HttpContext context, RequestDelegate _next)
    {
        if (context.Request.Headers.TryGetValue("Idempotency-Key", out var key))
        {
            if (cache.TryGetValue(key, out var response))
            {
                context.Response.ContentType = "application/json";

                if (response is not null)
                {
                    var resStr = response.ToString();

                    if (resStr is not null)
                        await context.Response.WriteAsync(resStr);
                }

                return;
            }

            var originalBodyStream = context.Response.Body;

            using var responseBody = new MemoryStream();

            context.Response.Body = responseBody;

            await _next(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);

            var text = await new StreamReader(context.Response.Body).ReadToEndAsync();

            cache.Set(key.ToString(), text, TimeSpan.FromMinutes(5));

            context.Response.Body.Seek(0, SeekOrigin.Begin);

            await responseBody.CopyToAsync(originalBodyStream);
        }
        else
        {
            await _next(context);
        }
    }
}