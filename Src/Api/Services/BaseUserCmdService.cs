using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using SharedKernel.Bl.Constants;
using SharedKernel.Bl.Contracts;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Dtos.User;
using SharedKernel.Da.Identity;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace SharedKernel.Api.Services;

/// <summary>
/// Provides commands and account management operations for application users,
/// including registration, authentication, email confirmation,
/// password reset, role assignment, and seller assignment.
/// </summary>
/// <remarks>
/// This service integrates with ASP.NET Core Identity
/// to handle authentication and authorization workflows.
/// </remarks>
/// <param name="emailSender">
/// Service responsible for sending application emails.
/// </param>
/// <param name="userManager">
/// ASP.NET Core Identity manager for user-related operations.
/// </param>
/// <param name="signInManager">
/// ASP.NET Core Identity manager for sign-in operations.
/// </param>
/// <param name="logger">
/// Logger for writing application log messages.
/// </param>
public abstract class BaseUserCmdService<TDbContext, AppUser>(
    IEmailing emailSender,
    IUserQry userQryService,
    UserManager<AppUser> userManager,
    IUserSessionService<TDbContext> userSessionService,
    IRefreshToken<TDbContext> refreshTokenService,
    SignInManager<AppUser> signInManager,
    ILogger<BaseUserCmdService<TDbContext, AppUser>> logger) :
    IUserCmd
    where TDbContext : DbContext
    where AppUser : SharedKernelAppUser, new()
{
    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="dto">
    /// Registration data transfer object.
    /// </param>
    /// <returns>
    /// A result describing whether registration succeeded.
    /// </returns>
    public async Task<UserResultDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("user: Registering user '{Email}'.", dto?.Email);

        if (dto is null)
            return UserResultDto.Fail("Dto cannot be null");

        if (dto.Password != dto.ConfirmPassword)
            return UserResultDto.Fail("Passwords do not match.");

        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PhoneNumber = dto.Phone
        };

        var result = await userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            logger.LogWarning("user: Registration of '{Email}' failed: {Errors}",
                dto.Email, string.Join("; ", result.Errors.Select(e => e.Description)));

            return UserResultDto.Fail(result.Errors.Select(e => e.Description).ToList());
        }

        List<string> roles = dto.Roles.Any() ? new List<string> { SystemRoles.User } : dto.Roles;

        foreach (var role in roles)
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);

            if (!roleResult.Succeeded)
            {
                logger.LogWarning("user: Assigning role '{Role}' to '{Email}' failed: {Errors}",
                    role, dto.Email, string.Join("; ", roleResult.Errors.Select(e => e.Description)));

                return UserResultDto.Fail(roleResult.Errors.Select(e => e.Description).ToList());
            }
        }

        await SendConfirmationEmail(user);

        logger.LogInformation("user: User '{Email}' registered successfully.", dto.Email);

        return new UserResultDto { Success = true };
    }

    /// <summary>
    /// Creates a new administrator account.
    /// </summary>
    /// <param name="dto">
    /// The administrator user data.
    /// </param>
    /// <returns>
    /// A result describing whether the operation succeeded.
    /// </returns>
    public async Task<UserResultDto> CreateAdminAsync(CreateUserDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("user: Creating admin user '{Email}'.", dto?.Email);

        return await RegisterAsync(new RegisterDto
        {
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Phone = dto.Phone,
            Password = dto.Password,
            ConfirmPassword = dto.ConfirmPassword,
            Roles = new List<string> { SystemRoles.Admin }
        }, cancellationToken);
    }

    /// <summary>
    /// Creates a standard user account.
    /// </summary>
    /// <param name="dto">
    /// The user data.
    /// </param>
    /// <returns>
    /// A result describing whether the operation succeeded.
    /// </returns>
    public async Task<UserResultDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("user: Creating user '{Email}'.", dto?.Email);

        return await RegisterAsync(new RegisterDto
        {
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Phone = dto.Phone,
            Password = dto.Password,
            ConfirmPassword = dto.ConfirmPassword,
            Roles = new List<string> { SystemRoles.User }
        }, cancellationToken);
    }

    /// <summary>
    /// Sends an email confirmation message to the specified user.
    /// </summary>
    /// <param name="user">
    /// The user receiving the confirmation email.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the generated token or user email is invalid.
    /// </exception>
    private async Task SendConfirmationEmail(AppUser user)
    {
        string? emailConfirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

        if (string.IsNullOrWhiteSpace(emailConfirmationToken))
            throw new ArgumentException("Email confirmation token cannot be null or empty.", nameof(emailConfirmationToken));

        if (string.IsNullOrWhiteSpace(user.Email))
            throw new ArgumentException("User email cannot be null or empty.", nameof(user.Email));

        string? tokenEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(emailConfirmationToken));

        logger.LogInformation("user: Sending confirmation email to '{Email}'.", user.Email);

        string? body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
  <style>
    body {{
        font-family: Arial, sans-serif;
        background-color: #f9f9f9;
        color: #333;
        padding: 20px;
    }}
    .container {{
        max-width: 600px;
        margin: 0 auto;
        background: #ffffff;
        border-radius: 8px;
        padding: 30px;
        box-shadow: 0 2px 6px rgba(0,0,0,0.1);
    }}
    h2 {{
        color: #2c3e50;
    }}
    p {{
        font-size: 15px;
        line-height: 1.6;
    }}
    .token-box {{
        margin-top: 20px;
        padding: 12px;
        background-color: #f4f4f4;
        border: 1px solid #ccc;
        border-radius: 5px;
        font-family: monospace;
        font-size: 14px;
        color: #2c3e50;
        word-break: break-all;
    }}
    .footer {{
        margin-top: 25px;
        font-size: 12px;
        color: #888;
        text-align: center;
    }}
  </style>
</head>
<body>
  <div class='container'>
    <h2>Welcome to Elite UK</h2>
    <p>Hello {user.FirstName},</p>
    <p>Here is your verification token:</p>

    <div class='token-box'>{tokenEncoded}</div>

    <div class='footer'>
      <p>If you did not create this account, you can safely ignore this email.</p>
    </div>
  </div>
</body>
</html>";

        await emailSender.SendAsync(user.Email ?? string.Empty, "Confirm your email", body);
    }

    /// <summary>
    /// Authenticates a user using email and password credentials.
    /// </summary>
    /// <param name="dto">
    /// Login request data.
    /// </param>
    /// <returns>
    /// A result describing whether authentication succeeded.
    /// </returns>
    public async Task<LogInResultDto> LogInAsync(
        LoginDto dto,
        string UserAgent,
        string IpAddress,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("user: Login attempt for '{Email}'.", dto?.Email);
        ArgumentNullException.ThrowIfNull(dto, nameof(dto));

        AppUser? user = await userManager.FindByEmailAsync(dto.Email);

        if (user == null || !await userManager.CheckPasswordAsync(user, dto.Password))
        {
            logger.LogWarning("user: Invalid login attempt for '{Email}'.", dto.Email);

            return LogInResultDto.Fail("Invalid login.");
        }

        if (!await userManager.IsEmailConfirmedAsync(user))
        {
            logger.LogWarning("user: Login for '{Email}' blocked - email not confirmed.", dto.Email);

            await SendConfirmationEmail(user);
            return LogInResultDto.Fail("Email not confirmed.");
        }

        if (await userManager.GetTwoFactorEnabledAsync(user))
        {
            logger.LogInformation("user: Login for '{Email}' requires 2FA.", dto.Email);

            return new UserResultDto { Success = true, Requires2FA = true };
        }

        logger.LogInformation("user: Login for '{Email}' succeeded.", dto.Email);

        return new UserResultDto
        {
            Success = true,
            Email = user.Email,
            UserId = user.Id
        };
    }

    /// <summary>
    /// Signs out the currently authenticated user.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation.
    /// </returns>
    public async Task LogoutAsync()
    {
        logger.LogInformation("user: User logging out.");

        await signInManager.SignOutAsync();
    }

    /// <summary>
    /// Confirms a user's email address using a confirmation token.
    /// </summary>
    /// <param name="email">
    /// The user email address.
    /// </param>
    /// <param name="token">
    /// The email confirmation token.
    /// </param>
    /// <returns>
    /// <c>true</c> if confirmation succeeds; otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> ConfirmEmailAsync(string email, string token)
    {
        logger.LogInformation("user: Confirming email for '{Email}'.", email);

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email)) return false;

        var user = await userManager.FindByEmailAsync(email);

        if (user == null) return false;

        var result = await userManager.ConfirmEmailAsync(user, Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token)));

        logger.LogInformation("user: Email confirmation for '{Email}' {Result}.", email, result.Succeeded ? "succeeded" : "failed");

        return result.Succeeded;
    }

    /// <summary>
    /// Sends a password reset token to the specified email address.
    /// </summary>
    /// <param name="email">
    /// The target email address.
    /// </param>
    /// <returns>
    /// <c>true</c> if the email exists and the token was sent;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the email or generated token is invalid.
    /// </exception>
    public async Task<bool> SendPasswordResetTokenAsync(string email)
    {
        logger.LogInformation("user: Sending password reset token to '{Email}'.", email);

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("User email cannot be null or empty.", nameof(email));

        AppUser? user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            logger.LogWarning("user: Password reset requested for unknown email '{Email}'.", email);

            return false;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Reset token cannot be null or empty.", nameof(token));

        string? tokenEncoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        string? body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='UTF-8'>
  <style>
    body {{
        font-family: Arial, sans-serif;
        background-color: #f4f6f8;
        color: #333;
        padding: 20px;
    }}
    .container {{
        max-width: 600px;
        margin: 0 auto;
        background: #ffffff;
        border-radius: 8px;
        padding: 30px;
        box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    }}
    h2 {{
        color: #c0392b;
    }}
    p {{
        font-size: 15px;
        line-height: 1.6;
    }}
    .token-box {{
        margin-top: 20px;
        padding: 12px;
        background-color: #f4f4f4;
        border: 1px solid #ccc;
        border-radius: 5px;
        font-family: monospace;
        font-size: 14px;
        color: #2c3e50;
        word-break: break-all;
    }}
    .footer {{
        margin-top: 25px;
        font-size: 12px;
        color: #888;
        text-align: center;
    }}
  </style>
</head>
<body>
  <div class='container'>
    <h2>Password Reset Requested</h2>
    <p>Hello,</p>
    <p>We received a request to reset the password for your account. Use the following token to complete the reset:</p>

    <div class='token-box'>{tokenEncoded}</div>

    <div class='footer'>
      <p>If you did not request this, you can safely ignore this email.</p>
    </div>
  </div>
</body>
</html>";

        await emailSender.SendAsync(email, "Reset Password", body);

        logger.LogInformation("user: Password reset token sent to '{Email}'.", email);

        return true;
    }

    /// <summary>
    /// Resets the password for a user account.
    /// </summary>
    /// <param name="dto">
    /// Password reset request data.
    /// </param>
    /// <returns>
    /// <c>true</c> if the password reset succeeds;
    /// otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        logger.LogInformation("user: Resetting password for '{Email}'.", dto?.Email);

        AppUser? user = await userManager.FindByEmailAsync(dto.Email);

        if (user == null)
            return true;

        IdentityResult? result = await userManager.ResetPasswordAsync(user, Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(dto.Token)), dto.NewPassword);

        logger.LogInformation("user: Password reset for '{Email}' {Result}.", dto.Email, result.Succeeded ? "succeeded" : "failed");

        return result.Succeeded;
    }

    #region Helpers

    /// <summary>
    /// Creates a failed user result containing a single error message.
    /// </summary>
    /// <param name="error">
    /// The error message.
    /// </param>
    /// <returns>
    /// A failed <see cref="UserResultDto"/> instance.
    /// </returns>
    private static UserResultDto Fail(string error) => new UserResultDto { Success = false, Errors = new List<string> { error } };

    /// <summary>
    /// Creates a failed user result containing multiple errors.
    /// </summary>
    /// <param name="errors">
    /// The collection of error messages.
    /// </param>
    /// <returns>
    /// A failed <see cref="UserResultDto"/> instance.
    /// </returns>
    private static UserResultDto Fail(List<string> errors) => new UserResultDto { Success = false, Errors = errors };

    #endregion
}