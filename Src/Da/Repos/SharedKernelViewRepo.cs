using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Da.Contracts;
using SharedKernel.Da.Contracts.Models;
using SharedKernel.Da.Exceptions;
using System.Linq.Expressions;

namespace SharedKernel.Da.Repos;

public class SharedKernelViewRepo<Vw, TDbContext>(
    TDbContext context,
    ILogger<SharedKernelViewRepo<Vw, TDbContext>> logger)
    : ISharedKernelViewRepo<Vw>
    where Vw : class
    where TDbContext : DbContext
{
    protected readonly DbSet<Vw> vwSet = (context ?? throw new ArgumentNullException(nameof(context))).Set<Vw>();

    public virtual async Task<IReadOnlyList<Vw>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await vwSet.AsNoTracking().ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            throw new DataAccessException(ex, $"Failed to retrieve all items of type {typeof(Vw).Name}");
        }
    }

    public virtual async Task<Vw?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return await vwSet.FindAsync(new object[] { id }, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to retrieve item of type {typeof(Vw).Name} with ID {id}");
        }
    }

    public virtual async Task<Vw?> GetFirstOrDefaultAsync(Expression<Func<Vw, bool>> filter, CancellationToken cancellationToken = default)
    {
        if (filter == null)
        {
            logger.LogWarning("Predicate cannot be null in {Method}", nameof(GetFirstOrDefaultAsync));
            throw new ArgumentNullException(nameof(filter));
        }

        try
        {
            return await vwSet.Where(filter).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to retrieve first matching item of type {typeof(Vw).Name}");
        }
    }

    public virtual async Task<IReadOnlyList<Vw>> GetListAsync(Expression<Func<Vw, bool>> filter, CancellationToken cancellationToken = default)
    {
        if (filter == null)
        {
            logger.LogWarning("Predicate cannot be null in {Method}", nameof(GetListAsync));
            throw new ArgumentNullException(nameof(filter));
        }

        try
        {
            return await vwSet.Where(filter).AsNoTracking().ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            throw new DataAccessException(ex, $"Failed to retrieve filtered list of type {typeof(Vw).Name}");
        }
    }

    public virtual async Task<IReadOnlyList<Vw>> FindAsync(Expression<Func<Vw, bool>> filter, CancellationToken cancellationToken)
        => await GetListAsync(filter, cancellationToken);


    public virtual async Task<int> CountAsync(
        Expression<Func<Vw, bool>>? filter,
        CancellationToken cancellationToken)
    {
        try
        {
            IQueryable<Vw> query = vwSet.AsQueryable();

            if (filter != null)
                query = query.Where(filter);

            return await query.CountAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            throw new DataAccessException(ex, $"Failed to count items of type {typeof(Vw).Name}");
        }
    }

    public virtual async Task<bool> IsExistsAsync(
        Expression<Func<Vw, bool>> filter,
        CancellationToken cancellationToken)
    {
        if (filter == null) throw new ArgumentNullException(nameof(filter));

        try
        {
            return await vwSet.AnyAsync(filter, cancellationToken);
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            throw new DataAccessException(ex, $"Failed in existence check for type {typeof(Vw).Name}");
        }
    }

}