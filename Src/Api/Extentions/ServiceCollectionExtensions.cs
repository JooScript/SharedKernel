using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Api.Middlewares;
using SharedKernel.Api.Services;
using SharedKernel.Bl.Contracts;
using SharedKernel.Bl.Contracts.AuditLog;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Contracts.Events;
using SharedKernel.Bl.Events.Publishers;
using SharedKernel.Bl.Services.AuditLog;
using SharedKernel.Bl.Services.Auth;
using SharedKernel.Bl.Services.Senders;
using SharedKernel.Da.Contracts;
using SharedKernel.Da.Repos;

namespace SharedKernel.Api.Extentions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedKernel(this IServiceCollection services)
    {
        services.AddMemoryCache();

        #region Middlewares

        services.AddTransient<ErrorHandlingMiddleware>();

        #endregion

        #region DI

        services.AddScoped(typeof(ISharedKernelTableQryRepo<,>), typeof(SharedKernelTableQryRepo<,>));
        services.AddScoped(typeof(ISharedKernelTableCmdRepo<,>), typeof(SharedKernelTableCmdRepo<,>));
        services.AddScoped(typeof(ISharedKernelUnitOfWork<>), typeof(SharedKernelUnitOfWork<>));

        services.AddScoped(typeof(IUserSessionService<>), typeof(UserSessionService<>));
        services.AddScoped(typeof(IRefreshToken<>), typeof(RefreshTokenService<>));
        services.AddScoped<IToken, TokenService>();
        services.AddScoped<IEmailing, EmailingService>();
        services.AddScoped<ICrudPublisher, CrudPublisher>();
        services.AddScoped(typeof(IAuditLog<>), typeof(AuditLogService<>));

        #endregion

        return services;
    }
}