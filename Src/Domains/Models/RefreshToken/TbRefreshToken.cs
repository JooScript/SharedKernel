namespace SharedKernel.Domains.Models.RefreshToken;

public class TbRefreshToken : SharedKernelBaseTable
{
    public string Token { get; set; } = null!;

    public DateTime Expires { get; set; }

}
