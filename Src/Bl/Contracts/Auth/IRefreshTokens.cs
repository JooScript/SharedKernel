using SharedKernel.Bl.Contracts.Base;
using SharedKernel.Bl.Dtos.User;
using SharedKernel.Domains.Models.RefreshToken;

namespace SharedKernel.Bl.Contracts.Auth;

public interface IRefreshTokens : IBaseService<TbRefreshToken, RefreshTokenDto>
{
    public Task<bool> Refresh(RefreshTokenDto tokenDto, CancellationToken cancellationToken = default);

    public Task<RefreshTokenDto> GetByToken(string token, CancellationToken cancellationToken = default);

}
