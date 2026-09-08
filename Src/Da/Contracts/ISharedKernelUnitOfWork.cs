using Microsoft.EntityFrameworkCore;

namespace SharedKernel.Da.Contracts;

public interface ISharedKernelUnitOfWork<TDbContext> : IAsyncDisposable
    where TDbContext : DbContext
{
    Task BeginTransactionAsync();

    Task CommitAsync();

    Task RollbackAsync();

    Task<int> SaveChangesAsync();
}
