using SharedKernel.Bl.Dtos.User;

namespace SharedKernel.Bl.Contracts.Auth;

public interface IUserCmd
{
    Task<UserResultDto> CreateAdminAsync(CreateUserDto registerDto);

    Task<UserResultDto> LoginAsync(LoginDto loginDto);

    Task LogoutAsync();

    Task<bool> ConfirmEmailAsync(string email, string token);

    Task<bool> SendPasswordResetTokenAsync(string email);

    Task<bool> ResetPasswordAsync(ResetPasswordDto model);

    Task<UserResultDto> CreateUserAsync(CreateUserDto dto);
}
