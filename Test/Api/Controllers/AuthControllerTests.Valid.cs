using Moq;
using SharedKernel.Bl.Dtos.User;

namespace SharedKernel.Api.Tests.Controllers;

public partial class AuthControllerTests
{
    [Fact]
    public async Task CreateAdmin_WhenServiceSucceeds_ReturnsOk()
    {
        _userCmd
            .Setup(command => command.CreateAdminAsync(It.IsAny<CreateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResultDto { Success = true });

        var result = await _sut.CreateAdmin(CreateUserDto(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Please Confirm The Email", response.Data);
    }

    [Fact]
    public async Task CreateUser_WhenServiceSucceeds_ReturnsOk()
    {
        _userCmd
            .Setup(command => command.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResultDto { Success = true });

        var result = await _sut.CreateUser(CreateUserDto(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Please Confirm The Email", response.Data);
    }

    [Fact]
    public async Task Register_WhenServiceSucceeds_ReturnsOk()
    {
        _userCmd
            .Setup(command => command.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserResultDto { Success = true });

        var result = await _sut.Register(CreateUserDto(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<string>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Please Confirm The Email", response.Data);
    }

    [Fact]
    public async Task LogIn_WhenSuccessful_ReturnsAccessTokenAndSetsCookies()
    {
        var dto = CreateLoginDto();
        var refreshToken = CreateRefreshTokenDto(token: "refresh-token");

        _userCmd
            .Setup(command => command.LogInAsync(dto, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LogInResultDto
            {
                UserResult = new UserResultDto
                {
                    Success = true,
                    Email = dto.Email,
                    UserId = UserId
                },
                UserData = CreateUser(),
                Claims = CreateClaims(),
                NewRefreshTokenDto = refreshToken
            });

        var result = await _sut.LogIn(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("Login successful", response.Message);
        _token.Verify(token => token.GenerateAccessToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>()), Times.Once);
        Assert.Contains("X-REFRESH-TOKEN", ResponseCookies);
        Assert.Contains("X-CSRF-TOKEN", ResponseCookies);
        Assert.Contains(refreshToken.Token, ResponseCookies);
        Assert.Contains(refreshToken.CSRFToken.ToString(), ResponseCookies);
    }

    [Fact]
    public async Task LogIn_WhenTwoFactorRequired_ReturnsTwoFactorMessage()
    {
        var dto = CreateLoginDto();

        _userCmd
            .Setup(command => command.LogInAsync(dto, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LogInResultDto
            {
                UserResult = new UserResultDto
                {
                    Success = true,
                    Requires2FA = true
                },
                NewRefreshTokenDto = CreateRefreshTokenDto(),
                Claims = CreateClaims()
            });

        var result = await _sut.LogIn(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<object>>(ok.Value);
        Assert.False(response.Success);
        Assert.Equal("2FA required", response.Message);
        Assert.Empty(ResponseCookies);
    }

    [Fact]
    public async Task LogOut_WhenCalled_ReturnsSuccessAndLogsOut()
    {
        _userCmd
            .Setup(command => command.LogoutAsync())
            .Returns(Task.CompletedTask);

        var result = await _sut.LogOut();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.True(response.Success);
        Assert.True(response.Data);
        _userCmd.Verify(command => command.LogoutAsync(), Times.Once);
    }

    [Fact]
    public async Task ConfirmEmail_WhenConfirmed_ReturnsOk()
    {
        _userCmd
            .Setup(command => command.ConfirmEmailAsync("user@example.com", "token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.ConfirmEmail("user@example.com", "token", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.True(response.Success);
        Assert.True(response.Data);
    }

    [Fact]
    public async Task ForgotPassword_WhenSent_ReturnsOk()
    {
        _userCmd
            .Setup(command => command.SendPasswordResetTokenAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.ForgotPassword(CreateForgotPasswordDto(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.True(response.Success);
        Assert.True(response.Data);
    }

    [Fact]
    public async Task ResetPassword_WhenReset_ReturnsOk()
    {
        _userCmd
            .Setup(command => command.ResetPasswordAsync(It.IsAny<ResetPasswordDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.ResetPassword(CreateResetPasswordDto(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<bool>>(ok.Value);
        Assert.True(response.Success);
        Assert.True(response.Data);
    }

    [Fact]
    public async Task RefreshAccessToken_WhenTokenIsValid_ReturnsNewAccessToken()
    {
        var csrf = Guid.NewGuid();
        SetRefreshCookie("refresh-token", csrf);

        _refreshToken
            .Setup(service => service.GetByToken("refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRefreshTokenDto(csrf));

        _userQry
            .Setup(query => query.GetClaims(UserId))
            .ReturnsAsync((CreateClaims(), CreateUser()));

        var result = await _sut.RefreshAccessToken(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ApiResponse<string>>(ok.Value);
        Assert.True(response.Success);
        Assert.Equal("access-token", response.Data);
        _userQry.Verify(query => query.GetClaims(UserId), Times.Once);
        _token.Verify(token => token.GenerateAccessToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>()), Times.Once);
    }

    [Fact]
    public async Task RefreshRefreshToken_WhenTokenIsValid_RotatesTokenAndReturnsNoContent()
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
            .ReturnsAsync(true);

        var result = await _sut.RefreshRefreshToken(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _refreshToken.Verify(service => service.GenerateRefreshToken(UserId, SessionId), Times.Once);
        _refreshToken.Verify(service => service.RefreshAsync(newRefreshToken, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("new-refresh-token", ResponseCookies);
    }
}
