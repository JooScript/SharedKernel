using Microsoft.AspNetCore.Http;
using SharedKernel.Bl.Contracts.Events;

namespace SharedKernel.Api.Middlewares;

/// <summary>
/// Middleware responsible for subscribing audit log handlers
/// to CRUD event publishers during the request pipeline execution.
/// </summary>
/// <remarks>
/// This middleware ensures that audit log subscribers are registered
/// before the request continues through the pipeline.
/// </remarks>
/// <param name="next">
/// The next middleware delegate in the HTTP request pipeline.
/// </param>
public class EventsSubscriptionMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invokes the middleware logic for the current HTTP request.
    /// </summary>
    /// <param name="context">
    /// The current HTTP context.
    /// </param>
    /// <param name="publisher">
    /// The CRUD event publisher responsible for publishing entity events.
    /// </param>
    /// <param name="subscriber">
    /// The audit log subscriber responsible for handling published events.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous middleware operation.
    /// </returns>
    public async Task Invoke(HttpContext context, ICrudPublisher publisher, IAuditLogSubscriber subscriber)
    {
        subscriber.Subscribe(publisher);
        await next(context);
    }
}