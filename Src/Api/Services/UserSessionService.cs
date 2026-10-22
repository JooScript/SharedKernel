using Microsoft.EntityFrameworkCore;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Da.Contracts;
using SharedKernel.Domains.Models.RefreshToken;
using SharedKernel.Domains.Types;

namespace SharedKernel.Api.Services;

public class UserSessionService<TDbContext>(
    ISharedKernelTableCmdRepo<TbUserSession, TDbContext> userSessionCmdRepo) :
    IUserSessionService<TDbContext>
    where TDbContext : DbContext
{
    public Task<SaveResultType> AddAsync(
        string UserAgent,
        string IpAddress,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return userSessionCmdRepo.AddAsync(new TbUserSession
        {
            UserId = userId,
            UserAgent = UserAgent,
            IpAddress = IpAddress,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }
}
