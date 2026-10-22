using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SharedKernel.Bl.Constants;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Dtos.User;
using SharedKernel.Bl.Exceptions;
using SharedKernel.Da.Identity;
using System.Security.Claims;

namespace SharedKernel.Api.Services;

/// <summary>
/// Provides read-only user query operations including retrieval of user data,
/// roles, and claim construction for authentication purposes.
/// </summary>
/// <remarks>
/// This service is responsible for mapping Identity user data
/// into application-level DTOs and building JWT-ready claims.
/// </remarks>
public abstract class BaseUserQryService<AppUser>(
    UserManager<AppUser> userManager,
    IHttpContextAccessor httpContextAccessor,
    ILogger<BaseUserQryService<AppUser>> logger) :
    IUserQry
    where AppUser : SharedKernelAppUser
{
    /// <summary>
    /// Gets the currently authenticated user ID from the HTTP context.
    /// </summary>
    /// <returns>
    /// The logged-in user identifier.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when the user ID cannot be retrieved from the current context.
    /// </exception>
    public Guid LoggedInUserId => GetLoggedInUserId();

    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="userId">
    /// The unique identifier of the user.
    /// </param>
    /// <returns>
    /// A <see cref="UserDto"/> if found; otherwise, null.
    /// </returns>
    public async Task<UserDto?> GetByIdAsync(Guid userId)
    {
        logger.LogInformation("user: Retrieving user with id {Id}.", userId);

        AppUser? user = await userManager.FindByIdAsync(userId.ToString());

        return user == null ? null : await ToUserDto(user);
    }

    /// <summary>
    /// Checks whether a user exists in the system.
    /// </summary>
    /// <param name="userId">
    /// The unique identifier of the user.
    /// </param>
    /// <returns>
    /// <c>true</c> if the user exists; otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> IsExistsAsync(Guid userId)
    {
        bool exists = await userManager.FindByIdAsync(userId.ToString()) != null;

        logger.LogInformation("user: Existence check for id {Id} returned {Result}.", userId, exists);

        return exists;
    }

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">
    /// The email address of the user.
    /// </param>
    /// <returns>
    /// A <see cref="UserDto"/> if found; otherwise, null.
    /// </returns>
    public async Task<UserDto?> GetByEmailAsync(string email)
    {
        logger.LogInformation("user: Retrieving user by email '{Email}'.", email);

        AppUser? user = await userManager.FindByEmailAsync(email);

        return user is null ? null : await ToUserDto(user);
    }

    /// <summary>
    /// Gets the currently authenticated user ID from the HTTP context.
    /// </summary>
    /// <returns>
    /// The logged-in user identifier.
    /// </returns>
    /// <exception cref="Exception">
    /// Thrown when the user ID cannot be retrieved from the current context.
    /// </exception>
    public Guid GetLoggedInUserId()
    {
        Guid userId = Guid.Parse(httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
            throw new UnauthorizedAccessException("Unable to Get UserId"));

        logger.LogInformation("user: Resolved logged-in user id {Id}.", userId);

        return userId;
    }

    #region Helpers

    /// <summary>
    /// Maps an <see cref="AppUser"/> entity to a <see cref="UserDto"/>.
    /// </summary>
    /// <param name="user">
    /// The identity user entity.
    /// </param>
    /// <returns>
    /// A populated <see cref="UserDto"/>.
    /// </returns>
    private async Task<UserDto> ToUserDto(AppUser user)
    {
        if (user.Email is null || user.UserName is null)
            throw new Exception("The Email Or UserName Can not be Null");

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.PhoneNumber,
            UserName = user.UserName,
            Roles = (List<string>)await userManager.GetRolesAsync(user)
        };
    }

    /// <summary>
    /// Builds authentication claims for a user.
    /// </summary>
    /// <param name="user">
    /// The user DTO.
    /// </param>
    /// <returns>
    /// A tuple containing claims and user data.
    /// </returns>
    private async Task<(Claim[] claims, UserDto user)> BuildUserClaims(UserDto user)
    {
        IEnumerable<string> roles = await GetUserRolesAsync(user.Id);

        List<Claim> claims = new List<Claim>
        {
            new Claim( ClaimTypes.NameIdentifier,user.Id.ToString()),
            new Claim( ClaimTypes.Name,user.UserName),
            new Claim( ClaimTypes.Email,user.Email)
        };

        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (!roles.Contains(SystemRoles.User))
        {
            claims.Add(new Claim(ClaimTypes.Role, SystemRoles.User));
        }

        return (claims.ToArray(), user);
    }

    #endregion

    /// <summary>
    /// Retrieves roles assigned to a specific user.
    /// </summary>
    /// <param name="userId">
    /// The user identifier.
    /// </param>
    /// <returns>
    /// A collection of role names.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the user does not exist.
    /// </exception>
    public async Task<IEnumerable<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user == null)
            throw new InvalidOperationException($"User with ID '{userId}' was not found.");


        var roles = await userManager.GetRolesAsync(user);

        logger.LogInformation("user: Retrieved roles for id {Id}: {Roles}.", userId, string.Join(", ", roles));

        return roles;
    }

    /// <summary>
    /// Builds claims for a user identified by email.
    /// </summary>
    /// <param name="email">
    /// The user email address.
    /// </param>
    /// <returns>
    /// A tuple containing claims and user data.
    /// </returns>
    public async Task<(Claim[] claims, UserDto user)> GetClaims(string email)
    {
        try
        {
            var user = await GetByEmailAsync(email);

            if (user is null)
                throw new Exception("The user can not be null");

            return await BuildUserClaims(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "user: Error occurred while getting claims for email '{Email}'.", email);

            throw new BusinessLogicException(ex, "Error occurred while getting claims");
        }
    }

    /// <summary>
    /// Builds claims for a user identified by ID.
    /// </summary>
    /// <param name="userId">
    /// The user identifier.
    /// </param>
    /// <returns>
    /// A tuple containing claims and user data.
    /// </returns>
    public async Task<(Claim[] claims, UserDto user)> GetClaims(Guid userId)
    {
        try
        {
            var user = await GetByIdAsync(userId);

            if (user is null)
                throw new Exception("The user can not be null");

            return await BuildUserClaims(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "user: Error occurred while getting claims for id {Id}.", userId);

            throw new BusinessLogicException(ex, "Error occurred while getting claims");
        }
    }
}