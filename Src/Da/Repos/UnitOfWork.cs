using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SharedKernel.Domains.Contracts.Generic;
using System.Collections.Concurrent;

namespace SharedKernel.Da.Repos;

public class UnitOfWork<TDbContext>(TDbContext ctx, ILoggerFactory loggerFactory)
    : IUnitOfWork<TDbContext>
    where TDbContext : DbContext
{
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private IDbContextTransaction? _tx;

    public ITableCmdRepo<Tb, TDbContext> Repository<Tb>() where Tb : SharedKernelBaseTable
    {
        return (ITableCmdRepo<Tb, TDbContext>)_repositories.GetOrAdd(typeof(Tb), _ => new TableCmdRepo<Tb, TDbContext>(ctx, loggerFactory.CreateLogger<TableCmdRepo<Tb, TDbContext>>(), new TableQryRepo<Tb, TDbContext>(ctx, loggerFactory.CreateLogger<TableQryRepo<Tb, TDbContext>>())));
    }

    public async Task BeginTransactionAsync() => _tx = await ctx.Database.BeginTransactionAsync();

    public async Task CommitAsync()
    {
        await ctx.SaveChangesAsync();
        if (_tx is not null) await _tx.CommitAsync();
    }

    public async Task RollbackAsync() => await _tx?.RollbackAsync()!;

    public Task<int> SaveChangesAsync() => ctx.SaveChangesAsync();

    public async ValueTask DisposeAsync()
    {
        if (_tx is not null) await _tx.DisposeAsync();
        await ctx.DisposeAsync();
    }

}
