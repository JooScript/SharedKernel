namespace SharedKernel.Domains.Models.RefreshToken;

public class TbRefreshToken : SharedKernelBaseTable
{
    public required string Token { get; set; } = null!;

    public required Guid CSRFToken { get; set; }

    public required DateTime Expires { get; set; }

    public required Guid SessionId { get; set; }
    public TbUserSession Session { get; set; } = null!;
}