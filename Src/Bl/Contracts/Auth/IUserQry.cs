using SharedKernel.Bl.Dtos.User;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;


namespace SharedKernel.Bl.Contracts.Auth;

public interface IUserQry
{
    Task<UserDto?> GetByIdAsync(Guid userId);

    Task<UserDto?> GetByEmailAsync(string email);

    Guid GetLoggedInUserId();

    Task<IEnumerable<string>> GetUserRolesAsync(Guid userId);

    Task<(Claim[] claims, UserDto user)> GetClaims(Guid userId);

    Task<(Claim[] claims, UserDto user)> GetClaims(string email);

    Task<bool> IsExistsAsync(Guid userId);

}
