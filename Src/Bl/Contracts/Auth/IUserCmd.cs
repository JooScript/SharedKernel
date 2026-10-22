using SharedKernel.Bl.Dtos.User;

namespace SharedKernel.Bl.Contracts.Auth;

public interface IUserCmd
{
    Task<UserResultDto> CreateAdminAsync(CreateUserDto dto, CancellationToken cancellationToken);

    Task<LogInResultDto> LogInAsync(LoginDto dto, string UserAgent, string IpAddress, CancellationToken cancellationToken);

    Task LogoutAsync();

    Task<bool> ConfirmEmailAsync(string email, string token, CancellationToken cancellationToken);

    Task<bool> SendPasswordResetTokenAsync(string email, CancellationToken cancellationToken);

    Task<bool> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken);

    Task<UserResultDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken);
}
