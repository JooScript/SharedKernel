using Microsoft.EntityFrameworkCore;
using SharedKernel.Domains.Types;

namespace SharedKernel.Bl.Contracts.Auth;

public interface IUserSessionService<TDbContext>
    where TDbContext : DbContext
{
    Task<SaveResultType> AddAsync(
        string UserAgent,
        string IpAddress,
        Guid userId,
        CancellationToken cancellationToken);
}