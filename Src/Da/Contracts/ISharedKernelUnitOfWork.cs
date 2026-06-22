using Microsoft.EntityFrameworkCore;

namespace SharedKernel.Da.Contracts;

public interface ISharedKernelUnitOfWork<TDbContext> : IAsyncDisposable
    where TDbContext : DbContext
{
    ISharedKernelTableCmdRepo<Tb, TDbContext> Repository<Tb>() where Tb : SharedKernelBaseTable;

    Task BeginTransactionAsync();

    Task CommitAsync();

    Task RollbackAsync();

    Task<int> SaveChangesAsync();
}
