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
            bool deactivateTokens = await DeactivateAllTokensAsync(tokenDto, cancellationToken);

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

    private async Task<bool> DeactivateAllTokensAsync(RefreshTokenDto tokenDto, CancellationToken cancellationToken)
    {
        try
        {
            var ids = await qryRepo.FindAsync(
                a => a.SessionId == tokenDto.SessionId && a.CurrentState == CurrentStateType.Active,
                a => a.Id,
                a => a.CreatedAt,
                true,
            cancellationToken);

            if (!ids.Any() || ids.All(id => id == Guid.Empty) || ids.Count() == 0) return true;

            return await cmdRepo.DeactivateAsync(ids, tokenDto.UserId, cancellationToken);
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

    public RefreshTokenDto GenerateRefreshToken(Guid userId, Guid sessionId)
    {
        return new RefreshTokenDto
        {
            Id = Guid.NewGuid(),
            Token = GenerateToken(),
            UserId = userId,
            SessionId = sessionId,
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
            SessionId = entity.SessionId
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
            SessionId = dto.SessionId
        };
    }


}
