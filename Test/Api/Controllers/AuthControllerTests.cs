using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SharedKernel.Api.Controllers.Auth;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Dtos.User;
using SharedKernel.Domains.Models.RefreshToken;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Api.Tests.Controllers;

public partial class AuthControllerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid SessionId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly Mock<IToken> _token = new();
    private readonly Mock<IUserQry> _userQry = new();
    private readonly Mock<IUserCmd> _userCmd = new();
    private readonly Mock<IRefreshToken<TestDbContext>> _refreshToken = new();
    private readonly AuthController<TestDbContext> _sut;

    public AuthControllerTests()
    {
        _token
            .Setup(token => token.GenerateAccessToken(It.IsAny<IEnumerable<Claim>>()))
            .Returns("access-token");

        _sut = new AuthController<TestDbContext>(
            _token.Object,
            _userQry.Object,
            _userCmd.Object,
            _refreshToken.Object,
            NullLogger<AuthController<TestDbContext>>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    private static CreateUserDto CreateUserDto() => new()
    {
        Email = "user@example.com",
        UserName = "user",
        Password = "Password1!",
        ConfirmPassword = "Password1!",
        FirstName = "First",
        LastName = "Last",
        Phone = "+966500000000"
    };

    private static LoginDto CreateLoginDto() => new()
    {
        Email = "user@example.com",
        Password = "Password1!"
    };

    private static ForgotPasswordDto CreateForgotPasswordDto() => new()
    {
        Email = "user@example.com"
    };

    private static ResetPasswordDto CreateResetPasswordDto() => new()
    {
        Email = "user@example.com",
        Token = "reset-token",
        NewPassword = "Password1!"
    };

    private static UserDto CreateUser() => new()
    {
        Id = UserId,
        Email = "user@example.com",
        UserName = "user",
        FirstName = "First",
        LastName = "Last",
        Phone = "+966500000000",
        CurrentState = CurrentStateType.Active,
        Roles = ["User"]
    };

    private static Claim[] CreateClaims() =>
    [
        new(ClaimTypes.NameIdentifier, UserId.ToString()),
        new(ClaimTypes.Email, "user@example.com")
    ];

    private static RefreshTokenDto CreateRefreshTokenDto(
        Guid? csrf = null,
        DateTime? expires = null,
        string token = "refresh-token",
        CurrentStateType state = CurrentStateType.Active) => new()
    {
        Id = Guid.NewGuid(),
        CurrentState = state,
        Token = token,
        CSRFToken = csrf ?? Guid.NewGuid(),
        UserId = UserId,
        SessionId = SessionId,
        Expires = expires ?? DateTime.UtcNow.AddDays(1)
    };

    private void SetRefreshCookie(string token, Guid? csrf = null)
    {
        _sut.ControllerContext.HttpContext.Request.Headers["Cookie"] = $"X-REFRESH-TOKEN={token}";

        if (csrf.HasValue)
            _sut.ControllerContext.HttpContext.Request.Headers["X-CSRF-TOKEN"] = csrf.Value.ToString();
    }

    private string ResponseCookies => _sut.Response.Headers.SetCookie.ToString();

    public sealed class TestDbContext : DbContext;
}
