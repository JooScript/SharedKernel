namespace SharedKernel.Bl.Dtos.User;

public class UserResultDto
{
    public bool Success { get; set; }

    public List<string> Errors { get; set; } = new();

    public string? Email { get; set; }

    public Guid UserId { get; set; }

    public bool Requires2FA { get; set; }

    #region Helpers

    /// <summary>
    /// Creates a failed user result containing a single error message.
    /// </summary>
    public static UserResultDto Fail(string error) =>
        new()
        {
            Success = false,
            Errors = new List<string> { error }
        };

    /// <summary>
    /// Creates a failed user result containing multiple errors.
    /// </summary>
    public static UserResultDto Fail(IEnumerable<string> errors) =>
        new()
        {
            Success = false,
            Errors = errors.ToList()
        };

    #endregion
}
