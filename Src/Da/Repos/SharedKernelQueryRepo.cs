using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Da.Contracts;
using SharedKernel.Da.Contracts.Models;
using SharedKernel.Da.Exceptions;
using System.Linq.Expressions;

namespace SharedKernel.Da.Repos;

public class SharedKernelQueryRepo<T, TDbContext>(TDbContext context) :
    ISharedKernelQueryRepo<T>
    where T : class
    where TDbContext : DbContext
{
    protected readonly DbSet<T> dbSet = (context ?? throw new ArgumentNullException(nameof(context))).Set<T>();

    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>>? filter,
        CancellationToken cancellationToken)
    {
        try
        {
            IQueryable<T> query = dbSet.AsQueryable();

            if (filter != null)
                query = query.Where(filter);

            return await query.CountAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            throw new DataAccessException(ex, $"Failed to count items of type {typeof(T).Name}");
        }
    }

    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>>? filter,
        CancellationToken cancellationToken)
    {
    }

    public virtual async Task<IEnumerable<TResult>> FindAsync<TResult>(
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>>? selector,
        Expression<Func<T, object>>? orderBy,
        bool isDescending,
        CancellationToken cancellationToken,
        params Expression<Func<T, object>>[] includes)
    {
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbSet.AsNoTracking().ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            throw new DataAccessException(ex, $"Failed to retrieve all items of type {typeof(T).Name}");
        }
    }

    public virtual async Task<T?> GetFirstOrDefaultAsync(
        Expression<Func<T, bool>>? filter,
        CancellationToken cancellationToken)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));

        try
        {
            return await dbSet.Where(filter).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to retrieve first matching item of type {typeof(T).Name}");
        }
    }

    public virtual async Task<PagedResult<TResult>> GetPagedListAsync<TResult>(int pageNumber,
        int pageSize, Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>>? selector,
        Expression<Func<T, object>>? orderBy,
        bool isDescending,
        CancellationToken cancellationToken,
        params Expression<Func<T, object>>[] includes)
    {
    }

    public virtual async Task<bool> IsExistsAsync(
        Expression<Func<T, bool>>? filter,
        CancellationToken cancellationToken)
    {
        if (filter == null)
            throw new ArgumentNullException(nameof(filter));

        try
        {
            return await dbSet.AnyAsync(filter, cancellationToken);
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            throw new DataAccessException(ex, $"Failed in existence check for type {typeof(T).Name}");
        }
    }

    public virtual async Task<TResult?> GetFirstOrDefaultAsync<TResult>(
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>>? selector,
        CancellationToken cancellationToken,
        params Expression<Func<T, object>>[] includes)
    {
    }

}
