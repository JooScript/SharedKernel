using Microsoft.EntityFrameworkCore;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Contracts.Events;
using SharedKernel.Bl.Dtos.User;
using SharedKernel.Bl.Services.Base;
using SharedKernel.Da.Contracts;
using SharedKernel.Domains.Models.RefreshToken;
using System.Security.Cryptography;
using Utils.General;
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
    IUserQry userQry,
    ICrudPublisher publisher)
    : base(repoQry, repoCmd, userQry)
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

    public async Task<RefreshTokenDto?> GetByToken(string token, CancellationToken cancellationToken = default)
    {
        var tokenEntity = await repoQry.GetFirstOrDefaultAsync(a => a.Token == token, cancellationToken);
        return tokenEntity != null ? ToDto(tokenEntity) : null;
    }

    /// <summary>
    /// Generates a cryptographically secure refresh token.
    /// </summary>
    /// <returns>
    /// A base64-encoded secure random refresh token.
    /// </returns>
    public string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];

        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            rng.GetBytes(randomNumber);

        return Convert.ToBase64String(randomNumber);
    }

    public override RefreshTokenDto ToDto(TbRefreshToken entity)
    {
        if (entity is null)
            throw new Exception("The entity can not be null");

        return new RefreshTokenDto
        {
            Id = entity.Id,
            CurrentState = entity.CurrentState,
            CSRFToken = entity.CSRFToken,
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
            Id = dto.Id.EmptyIfNull(),
            CurrentState = dto.CurrentState,
            CSRFToken = dto.CSRFToken,
            Token = dto.Token,
            Expires = dto.Expires,
            CreatedBy = dto.UserId,
        };
    }

}
