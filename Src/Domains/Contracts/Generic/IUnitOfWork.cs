namespace SharedKernel.Domains.Contracts.Generic;

public interface IUnitOfWork : IAsyncDisposable
{
    ITableCmdRepo<Tb> Repository<Tb>() where Tb : SharedKernelBaseTable;

    Task BeginTransactionAsync();

    Task CommitAsync();

    Task RollbackAsync();

    Task<int> SaveChangesAsync();
}
