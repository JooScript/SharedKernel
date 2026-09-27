using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SharedKernel.Da.Contracts;

namespace SharedKernel.Da.Repos;

public class SharedKernelUnitOfWork<TDbContext>(TDbContext context)
    : ISharedKernelUnitOfWork<TDbContext>
    where TDbContext : DbContext
{
    private IDbContextTransaction? _tx;

    public async Task BeginTransactionAsync() => _tx = await context.Database.BeginTransactionAsync();

    public async Task CommitAsync()
    {
        await context.SaveChangesAsync();

        if (_tx is not null)
            await _tx.CommitAsync();
    }

    public async Task RollbackAsync() => await _tx?.RollbackAsync()!;

    public async Task<int> SaveChangesAsync() => await context.SaveChangesAsync();

    public async ValueTask DisposeAsync()
    {
        if (_tx is not null)
        {
            await _tx.DisposeAsync();
            _tx = null;
        }

        await context.DisposeAsync();
    }

}
