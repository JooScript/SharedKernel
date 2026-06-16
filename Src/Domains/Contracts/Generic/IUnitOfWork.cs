using Microsoft.EntityFrameworkCore;

namespace SharedKernel.Domains.Contracts.Generic;

public interface IUnitOfWork<TDbContext> : IAsyncDisposable
    where TDbContext : DbContext
{
    ITableCmdRepo<Tb, TDbContext> Repository<Tb>() where Tb : SharedKernelBaseTable;

    Task BeginTransactionAsync();

    Task CommitAsync();

    Task RollbackAsync();

    Task<int> SaveChangesAsync();
}
