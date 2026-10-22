
using System.Security.Claims;

namespace SharedKernel.Bl.Dtos.User;

public class LogInResultDto
{
    public required UserResultDto UserResult { get; set; }

    public RefreshTokenDto? NewRefreshTokenDto { get; set; }

    public UserDto? UserData { get; set; }

    public Claim[]? Claims { get; set; }

    #region Helpers

    public static LogInResultDto Fail(string error) =>
        new()
        {
            UserResult = UserResultDto.Fail(error)
        };

    public static LogInResultDto Fail(IEnumerable<string> errors) =>
        new()
        {
            UserResult = UserResultDto.Fail(errors)
        };

    #endregion
}
