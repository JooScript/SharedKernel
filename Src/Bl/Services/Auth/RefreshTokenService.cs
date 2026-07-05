using Microsoft.EntityFrameworkCore;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Contracts.Events;
using SharedKernel.Bl.Dtos.User;
using SharedKernel.Bl.Services.Base;
using SharedKernel.Da.Contracts;
using SharedKernel.Domains.Models.RefreshToken;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Bl.Services.Auth;

public class RefreshTokenService<TDbContext>
    : SharedKernelBaseService<TbRefreshToken, RefreshTokenDto, TDbContext>,
    IRefreshTokens<TDbContext>
    where TDbContext : DbContext
{
    ISharedKernelTableCmdRepo<TbRefreshToken, TDbContext> repoCmd;
    ISharedKernelTableQryRepo<TbRefreshToken, TDbContext> repoQry;

    public RefreshTokenService(
    ISharedKernelTableQryRepo<TbRefreshToken, TDbContext> repoQry,
    ISharedKernelTableCmdRepo<TbRefreshToken, TDbContext> repoCmd,
    IUserQry userServiceQuery,
    ICrudPublisher publisher)
    : base(repoQry, repoCmd, userServiceQuery, publisher)
    {
        this.repoCmd = repoCmd;
        this.repoQry = repoQry;
    }

    public async Task<bool> Refresh(RefreshTokenDto tokenDto, CancellationToken cancellationToken = default)
    {
        List<TbRefreshToken>? tokenList = await repoQry.GetListAsync(a => a.CreatedBy == tokenDto.UserId && a.CurrentState == CurrentStateType.Active, cancellationToken);

        foreach (TbRefreshToken dbToken in tokenList)
        {
            await repoCmd.ChangeStatusAsync(dbToken.Id, tokenDto.UserId, CurrentStateType.InActive, cancellationToken);
        }

        TbRefreshToken dbTokens = ToEntity(tokenDto);

        dbTokens.CreatedBy = tokenDto.UserId;

        await repoCmd.AddAsync(dbTokens);
        return true;
    }

    public async Task<RefreshTokenDto> GetByToken(string token, CancellationToken cancellationToken = default)
        => ToDto(await repoQry.GetFirstOrDefaultAsync(a => a.Token == token)
            ?? throw new Exception($"There is No Token With This {token}"));

    public override RefreshTokenDto ToDto(TbRefreshToken entity)
    {
        if (entity is null)
            throw new Exception("The entity can not be null");

        return new RefreshTokenDto
        {
            Id = entity.Id,
            CurrentState = entity.CurrentState,
            Token = entity.Token,
            Expires = entity.Expires,
            UserId = entity.CreatedBy
        };
    }

    public override TbRefreshToken ToEntity(RefreshTokenDto dto)
    {
        if (dto == null)
            throw new Exception("The dto can not be null");

        return new TbRefreshToken
        {
            Id = dto.Id,
            CurrentState = dto.CurrentState,
            Token = dto.Token,
            Expires = dto.Expires,
            CreatedBy = dto.UserId,
        };
    }

}
