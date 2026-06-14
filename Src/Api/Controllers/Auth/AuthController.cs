using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Api.Models;
using SharedKernel.Bl.Constants;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Dtos.User;
using System.Security.Claims;
using static SharedKernel.Core.Enums.Status.Status;
namespace SharedKernel.Api.Controllers.Auth;

/// <summary>
/// Provides authentication and authorization endpoints including
/// user registration, login, logout, token refresh, email confirmation,
/// password reset, and two-factor authentication.
/// </summary>
/// <remarks>
/// This controller manages authentication flows using JWT access tokens
/// and refresh tokens stored in cookies.
/// Access tokens are short-lived, while refresh tokens are long-lived
/// and are persisted server-side to allow rotation and invalidation.
/// </remarks>
[Route("api/v1/auth")]
[ApiController]
[Authorize(Roles = SystemRoles.Admin)]
public class AuthController<TDbContext>(
    IToken tokenService,
    IUserQry userQryService,
    IUserCmd userCmdService,
    IRefreshTokens<TDbContext> refreshTokenService)
    : ControllerBase
    where TDbContext : DbContext
{
    private string RefreshTokenCookieName => "RefreshToken";

    private string AccessTokenCookieName => "AccessToken";

    /// <summary>
    /// Create a new admin.
    /// </summary>
    /// <param name="request">Admin registration data.</param>
    /// <returns>
    /// <see cref="IActionResult"/>
    /// - 200 with success message if registration succeeds.
    /// - 400 with validation or business errors if registration fails.
    /// </returns>
    [HttpPost("create-admin")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<string>>> CreateAdmin([FromBody] CreateUserDto request)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse<string>.FailResponse("Request body cannot be null."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.FailResponse(
                "Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        UserResultDto result = await userCmdService.CreateAdminAsync(request);

        if (result.Success)
        {
            ApiResponse<string>? response = ApiResponse<string>.SuccessResponse("Please Confirm The Email");
            return Ok(response);
        }

        ApiResponse<string>? errorResponse = ApiResponse<string>.FailResponse("Registration failed", result.Errors);
        return BadRequest(errorResponse);
    }

    /// <summary>
    /// Authenticates a user by validating their credentials and issuing access/refresh tokens.
    /// </summary>
    /// <param name="request">The login request containing email and password credentials.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing:
    /// <list type="bullet">
    /// <item>
    /// <description><b>200 OK</b> - Returns an access token and refresh token if login is successful.</description>
    /// </item>
    /// <item>
    /// <description><b>400 BadRequest</b> - Returned when the model validation fails.</description>
    /// </item>
    /// <item>
    /// <description><b>401 Unauthorized</b> - Returned if the login attempt fails due to invalid credentials.</description>
    /// </item>
    /// <item>
    /// <description><b>500 InternalServerError</b> - Returned when an unhandled error occurs.</description>
    /// </item>
    /// </list>
    ///
    /// <para>
    /// The refresh token is generated and stored in the database for tracking.
    /// Additionally, it is returned to the client in a secure <b>HttpOnly</b> cookie.
    /// The cookie’s name is defined by the <c>RefreshTokenCookieName</c> field/constant in the controller,
    /// and it currently uses a 7-day expiration.
    /// </para>
    /// </returns>
    /// <remarks>
    /// - If the account requires two-factor authentication (2FA), the response will indicate that explicitly.
    /// - The access token is included in the response body for client-side use.
    /// - The refresh token is persisted in a secure cookie, so clients should read it from there rather than relying on the response body.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> Login([FromBody] LoginDto request)
    {
        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse<object>.FailResponse("Validation failed", validationErrors));
        }

        UserResultDto? userResult = await userCmdService.LoginAsync(request);

        if (!userResult.Success)
            return Unauthorized(ApiResponse<object>.FailResponse("Login failed", userResult.Errors));

        if (userResult.Requires2FA)
        {
            return Ok(ApiResponse<object>.FailResponse("2FA required"));
        }

        (Claim[] claims, UserDto user) userData = await userQryService.GetClaims(request.Email);
        string? accessToken = tokenService.GenerateAccessToken(userData.claims);
        string? refreshToken = tokenService.GenerateRefreshToken();

        RefreshTokenDto? storedRefreshToken = new RefreshTokenDto
        {
            Id = Guid.NewGuid(),
            Token = refreshToken,
            UserId = userData.user.Id,
            Expires = DateTime.UtcNow.AddDays(7),
            CurrentState = enCurrentState.Active
        };

        await refreshTokenService.Refresh(storedRefreshToken);

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = storedRefreshToken.Expires
        });

        var loginResponse = new
        {
            userInfo = new
            {
                UserName = userData.user.UserName,
                Email = userData.user.Email,
                FirstName = userData.user.FirstName,
                LastName = userData.user.LastName,
            },
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };

        return Ok(ApiResponse<object>.SuccessResponse(loginResponse, "Login successful"));
    }

    /// <summary>
    /// Logs the current user out and clears authentication cookies.
    /// </summary>
    /// <returns>200 with success message.</returns>
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> Logout()
    {
        await userCmdService.LogoutAsync();

        Response.Cookies.Delete(RefreshTokenCookieName);
        Response.Cookies.Delete(AccessTokenCookieName);

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Logged out successfully."));
    }

    /// <summary>
    /// Confirms a user's email address using the confirmation token sent to their email.
    /// </summary>
    /// <param name="email">
    /// The email address of the user whose email is being confirmed.
    /// </param>
    /// <param name="token">
    /// The email confirmation token that was generated during registration
    /// and sent to the user's email.
    /// </param>
    /// <returns>
    /// An <see cref="IActionResult"/> that represents the result of the operation:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///     Returns <see cref="OkObjectResult"/> (HTTP 200) if the email confirmation succeeds.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     Returns <see cref="BadRequestObjectResult"/> (HTTP 400) if the email confirmation fails.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     Returns <see cref="UnauthorizedObjectResult"/> (HTTP 401) if the request is not authorized.
    ///     </description>
    ///   </item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// This endpoint is typically invoked when a user clicks the confirmation link
    /// sent to their registered email. The link should include both the <c>email</c>
    /// and <c>token</c> as query parameters.
    /// </remarks>
    [AllowAnonymous]
    [HttpGet("confirm-email")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> ConfirmEmail([FromQuery] string email, [FromQuery] string token)
    {
        bool result = await userCmdService.ConfirmEmailAsync(email, token);

        if (!result)
        {
            return BadRequest(ApiResponse<bool>.FailResponse("Email confirmation failed."));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Email confirmed successfully."));
    }

    /// <summary>
    /// Sends a password reset link to the user's registered email address.
    /// </summary>
    /// <param name="dto">
    /// A <see cref="ForgotPasswordDto"/> containing the user's email address.
    /// </param>
    /// <returns>
    /// An <see cref="IActionResult"/> representing the result of the operation:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///     Returns <see cref="OkObjectResult"/> (HTTP 200) if the reset link is sent successfully.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     Returns <see cref="BadRequestObjectResult"/> (HTTP 400) if validation fails
    ///     or the reset email could not be sent.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     Returns <see cref="UnauthorizedObjectResult"/> (HTTP 401) if the request is not authorized.
    ///     </description>
    ///   </item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// This endpoint should be used when a user has forgotten their password
    /// and requests a reset link. The service will generate a secure token
    /// and send it to the provided email if it is associated with an account.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword(ForgotPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<bool>.FailResponse(
                "Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        bool sent = await userCmdService.SendPasswordResetTokenAsync(dto.Email);

        if (!sent)
        {
            return BadRequest(ApiResponse<bool>.FailResponse("Failed to send password reset email."));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Password reset email sent successfully."));
    }

    /// <summary>
    /// Resets a user's password using a valid reset token.
    /// </summary>
    /// <param name="dto">
    /// A <see cref="ResetPasswordDto"/> containing the user's email, 
    /// the reset token, and the new password.
    /// </param>
    /// <returns>
    /// An <see cref="IActionResult"/> representing the result of the operation:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///     Returns <see cref="OkObjectResult"/> (HTTP 200) if the password is reset successfully.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     Returns <see cref="BadRequestObjectResult"/> (HTTP 400) if validation fails 
    ///     or the reset operation is unsuccessful (e.g., invalid token).
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     Returns <see cref="UnauthorizedObjectResult"/> (HTTP 401) if the request is not authorized.
    ///     </description>
    ///   </item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// This endpoint should be called after the user clicks the password reset link
    /// sent to their email. The client should submit the <c>email</c>, <c>token</c>,
    /// and the <c>new password</c> for the reset to succeed.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<bool>.FailResponse(
                "Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }
        bool result = await userCmdService.ResetPasswordAsync(dto);
        if (!result)
        {
            return BadRequest(ApiResponse<bool>.FailResponse("Failed to reset password."));
        }
        return Ok(ApiResponse<bool>.SuccessResponse(true, "Password reset successfully."));
    }

    /// <summary>
    /// Refreshes the access token using the refresh token stored in cookies.
    /// </summary>
    /// <remarks>
    /// This endpoint is typically used by clients when their access token expires.
    /// The refresh token must be valid, active, and not expired.
    /// A new short-lived access token is generated and returned, and also set in cookies.
    /// </remarks>
    /// <returns>
    /// An <see cref="IActionResult"/> representing the result of the operation:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///     Returns <see cref="OkObjectResult"/> (HTTP 200) with a new access token if the refresh token is valid.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     Returns <see cref="UnauthorizedObjectResult"/> (HTTP 401) if the refresh token is invalid, expired, or missing.
    ///     </description>
    ///   </item>
    /// </list>
    /// </returns>
    [AllowAnonymous]
    [HttpPost("refresh-access-token")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> RefreshAccessToken()
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out string? refreshToken))
        {
            return Unauthorized(ApiResponse<object>.FailResponse("No refresh token found."));
        }

        RefreshTokenDto? storedToken = await refreshTokenService.GetByToken(refreshToken);

        if (storedToken == null ||
            storedToken.CurrentState == enCurrentState.InActive ||
            storedToken.Expires < DateTime.UtcNow)
        {
            return Unauthorized(ApiResponse<object>.FailResponse("Invalid or expired refresh token."));
        }

        var claims = await userQryService.GetClaims(storedToken.UserId);
        var newAccessToken = tokenService.GenerateAccessToken(claims.claims);

        Response.Cookies.Append(AccessTokenCookieName, newAccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = DateTime.UtcNow.AddMinutes(15)
        });

        return Ok(ApiResponse<string>.SuccessResponse(newAccessToken, "Access token refreshed successfully."));
    }

    /// <summary>
    /// Issues a new refresh token, replacing the old one.
    /// </summary>
    /// <remarks>
    /// This endpoint performs <b>refresh token rotation</b>, which helps reduce the impact of token theft.
    /// A new refresh token is generated, persisted in the database, and stored in cookies.
    /// The old refresh token becomes invalid immediately.
    /// </remarks>
    /// <returns>
    /// An <see cref="IActionResult"/> representing the result of the operation:
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///     Returns <see cref="OkObjectResult"/> (HTTP 200) with a new refresh token if the old one is valid.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     Returns <see cref="UnauthorizedObjectResult"/> (HTTP 401) if the old refresh token is missing, invalid, or expired.
    ///     </description>
    ///   </item>
    /// </list>
    /// </returns>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<object>>> Refresh()
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out string? refreshToken))
        {
            return Unauthorized(ApiResponse<object>.FailResponse("No refresh token found."));
        }

        var storedToken = await refreshTokenService.GetByToken(refreshToken);

        if (storedToken == null ||
            storedToken.CurrentState == enCurrentState.InActive ||
            storedToken.Expires < DateTime.UtcNow)
        {
            return Unauthorized(ApiResponse<object>.FailResponse("Invalid or expired refresh token."));
        }

        string? newRefreshToken = tokenService.GenerateRefreshToken();

        RefreshTokenDto? newRefreshDto = new RefreshTokenDto
        {
            Id = Guid.NewGuid(),
            Token = newRefreshToken,
            UserId = storedToken.UserId,
            Expires = DateTime.UtcNow.AddDays(7),
            CurrentState = enCurrentState.Active
        };

        await refreshTokenService.Refresh(newRefreshDto);

        Response.Cookies.Append(RefreshTokenCookieName, newRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = newRefreshDto.Expires
        });

        return Ok(ApiResponse<string>.SuccessResponse(newRefreshToken, "Refresh token issued successfully."));
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">User registration data.</param>
    /// <returns>
    /// <see cref="IActionResult"/>
    /// - 200 with success message if registration succeeds.
    /// - 400 with validation or business errors if registration fails.
    /// </returns>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<string>>> Register([FromBody] CreateUserDto request)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse<string>.FailResponse("Request body cannot be null."));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<string>.FailResponse(
                "Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }
        UserResultDto result = await userCmdService.CreateUserAsync(request);

        if (result.Success)
        {
            var response = ApiResponse<string>.SuccessResponse("Please Confirm The Email");
            return Ok(response);
        }

        var errorResponse = ApiResponse<string>.FailResponse("Registration failed", result.Errors);
        return BadRequest(errorResponse);
    }

}