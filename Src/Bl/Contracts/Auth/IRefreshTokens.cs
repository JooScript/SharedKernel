using Microsoft.EntityFrameworkCore;
using SharedKernel.Bl.Contracts.Base;
using SharedKernel.Bl.Dtos.User;
using SharedKernel.Domains.Models.RefreshToken;

namespace SharedKernel.Bl.Contracts.Auth;

public interface IRefreshTokens<TDbContext>
    : IBaseService<TbRefreshToken, RefreshTokenDto>
    where TDbContext : DbContext
{
    public Task<bool> Refresh(RefreshTokenDto tokenDto, CancellationToken cancellationToken = default);

    public Task<RefreshTokenDto> GetByToken(string token, CancellationToken cancellationToken = default);

    string GenerateRefreshToken();
}
