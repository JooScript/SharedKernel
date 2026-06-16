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
using SharedKernel.Da.Repos;
using SharedKernel.Domains.Contracts.Generic;

namespace SharedKernel.Api.Extentions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSharedKernelApi(this IServiceCollection services)
    {
        services.AddMemoryCache();

        #region Middlewares

        services.AddTransient<ErrorHandlingMiddleware>();

        #endregion

        #region DI

        services.AddScoped(typeof(ITableQryRepo<,>), typeof(TableQryRepo<,>));
        services.AddScoped(typeof(ITableCmdRepo<,>), typeof(TableCmdRepo<,>));
        services.AddScoped(typeof(IUnitOfWork<>), typeof(UnitOfWork<>));

        services.AddScoped(typeof(IRefreshTokens<>), typeof(RefreshTokenService<>));
        services.AddScoped<IToken, TokenService>();
        services.AddScoped<IEmailing, EmailingService>();
        services.AddScoped<ICrudPublisher, CrudPublisher>();
        services.AddScoped(typeof(IAuditLog<>), typeof(AuditLogService<>));

        #endregion

        return services;
    }
}