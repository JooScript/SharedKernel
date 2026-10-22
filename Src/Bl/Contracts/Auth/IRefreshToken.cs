using Microsoft.EntityFrameworkCore;
using SharedKernel.Bl.Contracts.Base;
using SharedKernel.Bl.Dtos.User;
using SharedKernel.Domains.Models.RefreshToken;

namespace SharedKernel.Bl.Contracts.Auth;

public interface IRefreshToken<TDbContext>
    : IBaseService<TbRefreshToken, RefreshTokenDto>
    where TDbContext : DbContext
{
    public Task<bool> RefreshAsync(RefreshTokenDto tokenDto, CancellationToken cancellationToken);

    public Task<RefreshTokenDto?> GetByToken(string token, CancellationToken cancellationToken);

    RefreshTokenDto GenerateRefreshToken(Guid userId, Guid sessionId);

}
