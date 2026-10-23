using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Api.Models;
using SharedKernel.Bl.Constants;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Dtos.User;
using System.Security.Claims;
using static SharedKernel.Domains.Types.CurrentState;

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
    IRefreshToken<TDbContext> refreshTokenService,
    ILogger<AuthController<TDbContext>> logger,
    string refreshTokenCookieName = "X-REFRESH-TOKEN",
    string csrfTokenCookieName = "X-CSRF-TOKEN",
    string csrfTokenHeaderName = "X-CSRF-TOKEN") :
    ControllerBase
    where TDbContext : DbContext
{
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
    public async Task<ActionResult<ApiResponse<string>>> CreateAdmin([FromBody] CreateUserDto request, CancellationToken cancellationToken)
    {
        logger.LogInformation("CreateAdmin request received for email: {Email}", request?.Email);

        if (request == null)
        {
            logger.LogWarning("CreateAdmin request body is null.");
            return BadRequest(ApiResponse<string>.FailResponse("Request body cannot be null."));
        }

        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            logger.LogWarning("CreateAdmin validation failed for email: {Email}. Errors: {Errors}", request.Email, validationErrors);
            return BadRequest(ApiResponse<string>.FailResponse("Validation failed", validationErrors));
        }

        UserResultDto result = await userCmdService.CreateAdminAsync(request, cancellationToken);

        if (result.Success)
        {
            logger.LogInformation("Admin created successfully for email: {Email}", request.Email);
            ApiResponse<string>? response = ApiResponse<string>.SuccessResponse("Please Confirm The Email");
            return Ok(response);
        }

        logger.LogWarning("Admin creation failed for email: {Email}. Errors: {Errors}", request.Email, result.Errors);
        ApiResponse<string>? errorResponse = ApiResponse<string>.FailResponse("Registration failed", result.Errors);
        return BadRequest(errorResponse);
    }

    /// <summary>
    /// Create a new user.
    /// </summary>
    /// <param name="request">User registration data.</param>
    /// <returns>
    /// <see cref="IActionResult"/>
    /// - 200 with success message if registration succeeds.
    /// - 400 with validation or business errors if registration fails.
    /// </returns>
    [AllowAnonymous]
    [HttpPost("create-user")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<string>>> CreateUser([FromBody] CreateUserDto request, CancellationToken cancellationToken)
    {
        logger.LogInformation("CreateUser request received for email: {Email}", request?.Email);

        if (request == null)
        {
            logger.LogWarning("CreateUser request body is null.");
            return BadRequest(ApiResponse<string>.FailResponse("Request body cannot be null."));
        }

        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            logger.LogWarning("CreateUser validation failed for email: {Email}. Errors: {Errors}", request.Email, validationErrors);
            return BadRequest(ApiResponse<string>.FailResponse("Validation failed", validationErrors));
        }

        UserResultDto result = await userCmdService.CreateUserAsync(request, cancellationToken);

        if (result.Success)
        {
            logger.LogInformation("User created successfully for email: {Email}", request.Email);
            ApiResponse<string>? response = ApiResponse<string>.SuccessResponse("Please Confirm The Email");
            return Ok(response);
        }

        logger.LogWarning("User creation failed for email: {Email}. Errors: {Errors}", request.Email, result.Errors);
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
    public async Task<ActionResult<ApiResponse<object>>> LogIn([FromBody] LoginDto request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Login attempt for email: {Email}", request?.Email);

        if (!ModelState.IsValid || request is null)
        {
            logger.LogWarning("Login validation failed for email: {Email}. Errors: {Errors}", request?.Email, ModelStateErrors);
            return BadRequest(ApiResponse<object>.FailResponse("Validation failed", ModelStateErrors));
        }

        var logInRes = await userCmdService.LogInAsync(request, UserAgent, IpAddress, cancellationToken);

        if (!logInRes.UserResult.Success)
        {
            logger.LogWarning("Login failed for email: {Email}. Errors: {Errors}",
                request.Email, logInRes.UserResult.Errors);

            return Unauthorized(ApiResponse<object>.FailResponse("Login failed", logInRes.UserResult.Errors));
        }

        if (logInRes.UserResult.Requires2FA)
        {
            logger.LogInformation("Login requires 2FA for email: {Email}",
                request.Email);

            return Ok(ApiResponse<object>.FailResponse("2FA required"));
        }

        AppendCookies(logInRes.NewRefreshTokenDto);

        logger.LogInformation("Login successful for email: {Email}, UserId: {UserId}", request.Email, logInRes.UserData?.Id);

        return Ok(ApiResponse<object>.SuccessResponse(new
        {
            UserInfo = new
            {
                logInRes.UserData?.UserName,
                logInRes.UserData?.Email,
                logInRes.UserData?.FirstName,
                logInRes.UserData?.LastName,
            },
            AccessToken = tokenService.GenerateAccessToken(logInRes.Claims)
        }, "Login successful"));
    }

    /// <summary>
    /// Logs the current user out and clears authentication cookies.
    /// </summary>
    /// <returns>200 with success message.</returns>
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<bool>>> LogOut()
    {
        logger.LogInformation("Logout requested by user: {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));

        await userCmdService.LogoutAsync();

        Response.Cookies.Delete(refreshTokenCookieName);
        Response.Cookies.Delete(csrfTokenCookieName);

        logger.LogInformation("Logout completed for user: {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
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
    public async Task<ActionResult<ApiResponse<bool>>> ConfirmEmail([FromQuery] string email, [FromQuery] string token, CancellationToken cancellationToken)
    {
        logger.LogInformation("Email confirmation attempt for email: {Email}", email);

        bool result = await userCmdService.ConfirmEmailAsync(email, token, cancellationToken);

        if (!result)
        {
            logger.LogWarning("Email confirmation failed for email: {Email}", email);
            return BadRequest(ApiResponse<bool>.FailResponse("Email confirmation failed."));
        }

        logger.LogInformation("Email confirmed successfully for email: {Email}", email);
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
    public async Task<ActionResult<ApiResponse<bool>>> ForgotPassword(ForgotPasswordDto dto, CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            logger.LogWarning("Forgot password request received with null data.");
            return BadRequest(ApiResponse<bool>.FailResponse("Invalid request data."));
        }

        logger.LogInformation("Forgot password request received for email: {Email}", dto.Email);

        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            logger.LogWarning("Forgot password validation failed for email: {Email}. Errors: {Errors}", dto?.Email, validationErrors);
            return BadRequest(ApiResponse<bool>.FailResponse("Validation failed", validationErrors));
        }

        bool sent = await userCmdService.SendPasswordResetTokenAsync(dto.Email, cancellationToken);

        if (!sent)
        {
            logger.LogWarning("Failed to send password reset email for email: {Email}", dto.Email);
            return BadRequest(ApiResponse<bool>.FailResponse("Failed to send password reset email."));
        }

        logger.LogInformation("Password reset email sent for email: {Email}", dto.Email);
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
    public async Task<ActionResult<ApiResponse<bool>>> ResetPassword(ResetPasswordDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Password reset attempt for email: {Email}", dto?.Email);

        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            logger.LogWarning("Password reset validation failed for email: {Email}. Errors: {Errors}", dto?.Email, validationErrors);
            return BadRequest(ApiResponse<bool>.FailResponse("Validation failed", validationErrors));
        }

        if (!(dto is not null && await userCmdService.ResetPasswordAsync(dto, cancellationToken)))
        {
            logger.LogWarning("Password reset failed for email: {Email}", dto?.Email);
            return BadRequest(ApiResponse<bool>.FailResponse("Failed to reset password."));
        }

        logger.LogInformation("Password reset successful for email: {Email}", dto.Email);
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
    public async Task<IActionResult> RefreshAccessToken(CancellationToken cancellationToken)
    {
        logger.LogInformation("Refresh access token request received.");

        if (!Request.Cookies.TryGetValue(refreshTokenCookieName, out string? refreshToken))
        {
            logger.LogWarning("Refresh access token failed: no refresh token cookie found.");
            return Unauthorized(ApiResponse<object>.FailResponse("No refresh token found."));
        }

        var storedRefreshTokenDto = await refreshTokenService.GetByToken(refreshToken, cancellationToken);

        string? storedCsrfToken = Request.Headers[csrfTokenHeaderName].FirstOrDefault();

        if (storedRefreshTokenDto == null ||
            storedRefreshTokenDto.CurrentState == CurrentStateType.InActive ||
            storedRefreshTokenDto.CSRFToken.ToString() != storedCsrfToken ||
            storedRefreshTokenDto.Expires < DateTime.UtcNow)
        {
            logger.LogWarning("Refresh access token failed: invalid or expired refresh token. UserId: {UserId}", storedRefreshTokenDto?.UserId);
            return Unauthorized(ApiResponse<object>.FailResponse("Invalid or expired refresh token."));
        }

        var claims = await userQryService.GetClaims(storedRefreshTokenDto.UserId);
        var newAccessToken = tokenService.GenerateAccessToken(claims.claims);

        logger.LogInformation("Access token refreshed successfully for UserId: {UserId}", storedRefreshTokenDto.UserId);
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
    [HttpPost("refresh-refresh-token")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshRefreshToken(CancellationToken cancellationToken)
    {
        logger.LogInformation("Refresh refresh token request received.");

        if (!Request.Cookies.TryGetValue(refreshTokenCookieName, out string? refreshToken))
        {
            logger.LogWarning("Refresh refresh token failed: no refresh token cookie found.");
            return Unauthorized(ApiResponse<object>.FailResponse("No refresh token found."));
        }

        var storedRefreshToken = await refreshTokenService.GetByToken(refreshToken, cancellationToken);

        string? storedCsrfToken = Request.Headers[csrfTokenHeaderName].FirstOrDefault();

        if (storedRefreshToken == null ||
            storedRefreshToken.CurrentState == CurrentStateType.InActive ||
            storedRefreshToken.CSRFToken.ToString() != storedCsrfToken ||
            storedRefreshToken.Expires < DateTime.UtcNow)
        {
            logger.LogWarning("Refresh refresh token failed: invalid or expired refresh token. UserId: {UserId}", storedRefreshToken?.UserId);
            return Unauthorized(ApiResponse<object>.FailResponse("Invalid or expired refresh token."));
        }

        var newRefreshDto = refreshTokenService.GenerateRefreshToken(storedRefreshToken.UserId, storedRefreshToken.SessionId);

        if (!await refreshTokenService.RefreshAsync(newRefreshDto, cancellationToken))
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to refresh token.");

        AppendCookies(newRefreshDto);

        logger.LogInformation("Refresh token rotated successfully for UserId: {UserId}", storedRefreshToken.UserId);
        return NoContent();
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
    public async Task<ActionResult<ApiResponse<string>>> Register([FromBody] CreateUserDto request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Register request received for email: {Email}", request?.Email);

        if (request == null)
        {
            logger.LogWarning("Register request body is null.");
            return BadRequest(ApiResponse<string>.FailResponse("Request body cannot be null."));
        }

        if (!ModelState.IsValid)
        {
            var validationErrors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            logger.LogWarning("Register validation failed for email: {Email}. Errors: {Errors}", request.Email, validationErrors);
            return BadRequest(ApiResponse<string>.FailResponse("Validation failed", validationErrors));
        }
        UserResultDto result = await userCmdService.CreateUserAsync(request, cancellationToken);

        if (result.Success)
        {
            logger.LogInformation("User registered successfully for email: {Email}", request.Email);
            var response = ApiResponse<string>.SuccessResponse("Please Confirm The Email");
            return Ok(response);
        }

        logger.LogWarning("User registration failed for email: {Email}. Errors: {Errors}", request.Email, result.Errors);
        var errorResponse = ApiResponse<string>.FailResponse("Registration failed", result.Errors);
        return BadRequest(errorResponse);
    }

    private void AppendCookies(RefreshTokenDto newRefreshDto)
    {
        Response.Cookies.Append(refreshTokenCookieName, newRefreshDto.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = newRefreshDto.Expires,
            IsEssential = true
        });

        Response.Cookies.Append(csrfTokenCookieName, newRefreshDto.CSRFToken.ToString(), new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = newRefreshDto.Expires,
            IsEssential = true
        });
    }

    private IEnumerable<string> ModelStateErrors =>
        ModelState.Values.
        SelectMany(v => v.Errors).
        Select(e => e.ErrorMessage);

    protected string UserAgent =>
    HttpContext.Request.Headers.UserAgent.ToString();

    protected string IpAddress =>
        HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();

}