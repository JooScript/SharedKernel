using Microsoft.EntityFrameworkCore;
using SharedKernel.Bl.Contracts.Auth;
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
    IRefreshToken<TDbContext>
    where TDbContext : DbContext
{
    ISharedKernelTableCmdRepo<TbRefreshToken, TDbContext> cmdRepo;
    ISharedKernelTableQryRepo<TbRefreshToken, TDbContext> qryRepo;

    public RefreshTokenService(
    ISharedKernelTableQryRepo<TbRefreshToken, TDbContext> qryRepo,
    ISharedKernelTableCmdRepo<TbRefreshToken, TDbContext> cmdRepo,
    IUserQry userQry)
    : base(cmdRepo, qryRepo, userQry)
    {
        this.cmdRepo = cmdRepo;
        this.qryRepo = qryRepo;
    }

    public async Task<bool> RefreshAsync(RefreshTokenDto tokenDto, CancellationToken cancellationToken)
    {
        try
        {
            bool deactivateTokens = await DeactivateAllTokensAsync(tokenDto.UserId, cancellationToken);

            if (!deactivateTokens)
                throw new Exception("Token deactivation failed");

            var dbTokens = ToEntity(tokenDto);

            dbTokens.CreatedBy = tokenDto.UserId;

            return (await cmdRepo.AddAsync(dbTokens, cancellationToken)).Success;
        }
        catch (Exception ex)
        {
            throw new Exception("Error while token refreshing", ex);
        }
    }

    private async Task<bool> DeactivateAllTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var tokenList = await qryRepo.FindAsync(a =>
            a.CreatedBy == userId &&
            a.CurrentState != CurrentStateType.InActive,
            cancellationToken);

            var ids = tokenList.Select(s => s.Id);

            return await cmdRepo.DeactivateAsync(ids, userId, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception("Error while token refreshing", ex);
        }
    }



    public async Task<RefreshTokenDto?> GetByToken(string token, CancellationToken cancellationToken)
    {
        var tokenEntity = await qryRepo.GetFirstOrDefaultAsync(a => a.Token == token, cancellationToken);

        return tokenEntity != null ? ToDto(tokenEntity) : null;
    }

    public RefreshTokenDto GenerateRefreshToken(Guid userId)
    {
        return new RefreshTokenDto
        {
            Id = Guid.NewGuid(),
            Token = GenerateToken(),
            UserId = userId,
            CSRFToken = Guid.NewGuid(),
            Expires = DateTime.UtcNow.AddDays(7),
            CurrentState = CurrentStateType.Active
        };
    }

    /// <summary>
    /// Generates a cryptographically secure refresh token.
    /// </summary>
    /// <returns>
    /// A base64-encoded secure random refresh token.
    /// </returns>
    public string GenerateToken()
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
            UserId = entity.CreatedBy,
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
