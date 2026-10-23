using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using SharedKernel.Api.Controllers.Auth;
using SharedKernel.Bl.Constants;

namespace SharedKernel.Api.Tests.Controllers;

public class AuthControllerAttributesTests
{
    [Fact]
    public void Controller_RequiresAdminRoleAndHasExpectedRoute()
    {
        var type = typeof(AuthController<AttributesTestDbContext>);

        var authorize = type.GetCustomAttribute<AuthorizeAttribute>();
        var apiController = type.GetCustomAttribute<ApiControllerAttribute>();
        var route = type.GetCustomAttribute<RouteAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal(SystemRoles.Admin, authorize.Roles);
        Assert.NotNull(apiController);
        Assert.NotNull(route);
        Assert.Equal("api/v1/auth", route.Template);
    }

    [Theory]
    [InlineData(nameof(AuthController<AttributesTestDbContext>.CreateUser))]
    [InlineData(nameof(AuthController<AttributesTestDbContext>.LogIn))]
    [InlineData(nameof(AuthController<AttributesTestDbContext>.ConfirmEmail))]
    [InlineData(nameof(AuthController<AttributesTestDbContext>.ForgotPassword))]
    [InlineData(nameof(AuthController<AttributesTestDbContext>.ResetPassword))]
    [InlineData(nameof(AuthController<AttributesTestDbContext>.RefreshAccessToken))]
    [InlineData(nameof(AuthController<AttributesTestDbContext>.RefreshRefreshToken))]
    [InlineData(nameof(AuthController<AttributesTestDbContext>.Register))]
    public void AnonymousEndpoint_AllowsAnonymousAccess(string methodName)
    {
        var method = typeof(AuthController<AttributesTestDbContext>)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method);
        Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public void AdminEndpoints_DoNotAllowAnonymousAccess()
    {
        var type = typeof(AuthController<AttributesTestDbContext>);

        Assert.Null(type.GetMethod(nameof(AuthController<AttributesTestDbContext>.CreateAdmin))!
            .GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Null(type.GetMethod(nameof(AuthController<AttributesTestDbContext>.LogOut))!
            .GetCustomAttribute<AllowAnonymousAttribute>());
    }

    private sealed class AttributesTestDbContext : Microsoft.EntityFrameworkCore.DbContext;
}
