using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Api.Extentions;
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

namespace SharedKernel.Api.Tests.Extentions;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSharedKernel_ReturnsSameCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddSharedKernel();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddSharedKernel_RegistersMemoryCache()
    {
        var services = AddServices();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IMemoryCache) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddSharedKernel_RegistersErrorHandlingMiddlewareAsTransient()
    {
        var services = AddServices();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(ErrorHandlingMiddleware) &&
            descriptor.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddSharedKernel_RegistersOpenGenericDataServicesAsScoped()
    {
        var services = AddServices();

        AssertOpenGeneric(services, typeof(ISharedKernelTableQryRepo<,>), ServiceLifetime.Scoped);
        AssertOpenGeneric(services, typeof(ISharedKernelTableCmdRepo<,>), ServiceLifetime.Scoped);
        AssertOpenGeneric(services, typeof(ISharedKernelUnitOfWork<>), ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSharedKernel_RegistersBusinessServices()
    {
        var services = AddServices();

        AssertOpenGeneric(services, typeof(IUserSessionService<>), ServiceLifetime.Scoped);
        AssertOpenGeneric(services, typeof(IRefreshToken<>), ServiceLifetime.Scoped);
        AssertOpenGeneric(services, typeof(IAuditLog<>), ServiceLifetime.Scoped);

        AssertDescriptor<IToken, TokenService>(services, ServiceLifetime.Scoped);
        AssertDescriptor<IEmailing, EmailingService>(services, ServiceLifetime.Scoped);
        AssertDescriptor<ICrudPublisher, CrudPublisher>(services, ServiceLifetime.Scoped);
    }

    private static IServiceCollection AddServices()
    {
        var services = new ServiceCollection();
        services.AddSharedKernel();
        return services;
    }

    private static void AssertOpenGeneric(
        IServiceCollection services,
        Type serviceType,
        ServiceLifetime lifetime)
    {
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType.IsGenericType &&
            descriptor.ServiceType.GetGenericTypeDefinition() == serviceType &&
            descriptor.Lifetime == lifetime);
    }

    private static void AssertDescriptor<TService, TImplementation>(
        IServiceCollection services,
        ServiceLifetime lifetime)
        where TImplementation : class, TService
    {
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(TService) &&
            descriptor.ImplementationType == typeof(TImplementation) &&
            descriptor.Lifetime == lifetime);
    }
}
