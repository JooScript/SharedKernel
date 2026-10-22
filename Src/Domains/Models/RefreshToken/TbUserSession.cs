namespace SharedKernel.Domains.Models.RefreshToken;

public class TbUserSession : SharedKernelBaseTable
{
    public Guid UserId { get; set; }

    public string UserAgent { get; set; } = null!;

    public string IpAddress { get; set; } = null!;

    public DateTime? LastUsedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual ICollection<TbRefreshToken> RefreshTokens { get; set; }
        = new List<TbRefreshToken>();
}