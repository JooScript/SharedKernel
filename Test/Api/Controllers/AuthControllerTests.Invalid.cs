using Moq;
using SharedKernel.Bl.Dtos.User;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Api.Tests.Controllers;

public partial class AuthControllerTests
{
    [Fact]
    public async Task CreateAdmin_WhenRequestIsNull_ReturnsBadRequest()
    {
        var result = await _sut.CreateAdmin(null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateAdmin_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        _sut.ModelState.AddModelError("Email", "Email is required");

        var result = await _sut.CreateAdmin(CreateUserDto(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);
        Assert.Equal("Validation failed", response.Message);
        Assert.Contains("Email is required", response.Errors!);
    }

    [Fact]
    public async Task CreateAdmin_WhenServiceFails_ReturnsBadRequest()
    {
        _userCmd
            .Setup(command => command.CreateAdminAsync(It.IsAny<CreateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserResultDto.Fail("email already exists"));

        var result = await _sut.CreateAdmin(CreateUserDto(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("Registration failed", response.Message);
        Assert.Contains("email already exists", response.Errors!);
    }

    [Fact]
    public async Task CreateUser_WhenRequestIsNull_ReturnsBadRequest()
    {
        var result = await _sut.CreateUser(null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        _sut.ModelState.AddModelError("Email", "Email is required");

        var result = await _sut.CreateUser(CreateUserDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateUser_WhenServiceFails_ReturnsBadRequest()
    {
        _userCmd
            .Setup(command => command.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserResultDto.Fail("creation failed"));

        var result = await _sut.CreateUser(CreateUserDto(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Contains("creation failed", response.Errors!);
    }

    [Fact]
    public async Task Register_WhenRequestIsNull_ReturnsBadRequest()
    {
        var result = await _sut.Register(null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        _sut.ModelState.AddModelError("Email", "Email is required");

        var result = await _sut.Register(CreateUserDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task Register_WhenServiceFails_ReturnsBadRequest()
    {
        _userCmd
            .Setup(command => command.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserResultDto.Fail("registration failed"));

        var result = await _sut.Register(CreateUserDto(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Contains("registration failed", response.Errors!);
    }

    [Fact]
    public async Task LogIn_WhenRequestIsNull_ReturnsBadRequest()
    {
        var result = await _sut.LogIn(null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task LogIn_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        _sut.ModelState.AddModelError("Email", "Email is required");

        var result = await _sut.LogIn(CreateLoginDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task LogIn_WhenCredentialsAreInvalid_ReturnsUnauthorized()
    {
        var dto = CreateLoginDto();

        _userCmd
            .Setup(command => command.LogInAsync(dto, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LogInResultDto.Fail("Invalid login."));

        var result = await _sut.LogIn(dto, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorized.Value);
        Assert.False(response.Success);
        Assert.Equal("Login failed", response.Message);
    }

    [Fact]
    public async Task ConfirmEmail_WhenConfirmationFails_ReturnsBadRequest()
    {
        _userCmd
            .Setup(command => command.ConfirmEmailAsync("user@example.com", "token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.ConfirmEmail("user@example.com", "token", CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("Email confirmation failed.", response.Message);
    }

    [Fact]
    public async Task ForgotPassword_WhenRequestIsNull_ReturnsBadRequest()
    {
        var result = await _sut.ForgotPassword(null!, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ForgotPassword_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        _sut.ModelState.AddModelError("Email", "Email is required");

        var result = await _sut.ForgotPassword(CreateForgotPasswordDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ForgotPassword_WhenNotSent_ReturnsBadRequest()
    {
        _userCmd
            .Setup(command => command.SendPasswordResetTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.ForgotPassword(CreateForgotPasswordDto(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("Failed to send password reset email.", response.Message);
    }

    [Fact]
    public async Task ResetPassword_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        _sut.ModelState.AddModelError("Email", "Email is required");

        var result = await _sut.ResetPassword(CreateResetPasswordDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ResetPassword_WhenResetFails_ReturnsBadRequest()
    {
        _userCmd
            .Setup(command => command.ResetPasswordAsync(It.IsAny<ResetPasswordDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.ResetPassword(CreateResetPasswordDto(), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("Failed to reset password.", response.Message);
    }

    [Fact]
    public async Task RefreshAccessToken_WhenCookieIsMissing_ReturnsUnauthorized()
    {
        var result = await _sut.RefreshAccessToken(CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorized.Value);
        Assert.Equal("No refresh token found.", response.Message);
    }

    [Fact]
    public async Task RefreshAccessToken_WhenTokenIsNotFound_ReturnsUnauthorized()
    {
        SetRefreshCookie("missing-token");

        _refreshToken
            .Setup(service => service.GetByToken("missing-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenDto?)null);

        var result = await _sut.RefreshAccessToken(CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorized.Value);
        Assert.Equal("Invalid or expired refresh token.", response.Message);
    }

    [Fact]
    public async Task RefreshAccessToken_WhenTokenIsInactive_ReturnsUnauthorized()
    {
        var csrf = Guid.NewGuid();
        SetRefreshCookie("refresh-token", csrf);

        _refreshToken
            .Setup(service => service.GetByToken("refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRefreshTokenDto(csrf, state: CurrentStateType.InActive));

        var result = await _sut.RefreshAccessToken(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task RefreshAccessToken_WhenCsrfTokenDoesNotMatch_ReturnsUnauthorized()
    {
        var csrf = Guid.NewGuid();
        SetRefreshCookie("refresh-token", csrf);
        _sut.ControllerContext.HttpContext.Request.Headers["X-CSRF-TOKEN"] = Guid.NewGuid().ToString();

        _refreshToken
            .Setup(service => service.GetByToken("refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRefreshTokenDto(csrf));

        var result = await _sut.RefreshAccessToken(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task RefreshAccessToken_WhenTokenIsExpired_ReturnsUnauthorized()
    {
        var csrf = Guid.NewGuid();
        SetRefreshCookie("refresh-token", csrf);

        _refreshToken
            .Setup(service => service.GetByToken("refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRefreshTokenDto(csrf, DateTime.UtcNow.AddMinutes(-1)));

        var result = await _sut.RefreshAccessToken(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task RefreshRefreshToken_WhenCookieIsMissing_ReturnsUnauthorized()
    {
        var result = await _sut.RefreshRefreshToken(CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        var response = Assert.IsType<ApiResponse<object>>(unauthorized.Value);
        Assert.Equal("No refresh token found.", response.Message);
    }

    [Fact]
    public async Task RefreshRefreshToken_WhenTokenIsNotFound_ReturnsUnauthorized()
    {
        SetRefreshCookie("missing-token");

        _refreshToken
            .Setup(service => service.GetByToken("missing-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenDto?)null);

        var result = await _sut.RefreshRefreshToken(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task RefreshRefreshToken_WhenRefreshFails_ReturnsInternalServerError()
    {
        var csrf = Guid.NewGuid();
        var newRefreshToken = CreateRefreshTokenDto(csrf, token: "new-refresh-token");
        SetRefreshCookie("refresh-token", csrf);

        _refreshToken
            .Setup(service => service.GetByToken("refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRefreshTokenDto(csrf));

        _refreshToken
            .Setup(service => service.GenerateRefreshToken(UserId, SessionId))
            .Returns(newRefreshToken);

        _refreshToken
            .Setup(service => service.RefreshAsync(newRefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.RefreshRefreshToken(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
    }
}
