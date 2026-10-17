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

    public virtual async Task<Vw?> GetFirstOrDefaultAsync(Expression<Func<Vw, bool>> filter, CancellationToken cancellationToken)
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

    public virtual async Task<IReadOnlyList<Vw>> GetListAsync(Expression<Func<Vw, bool>> filter, CancellationToken cancellationToken)
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

    public virtual async Task<IReadOnlyList<TResult>> GetListAsync<TResult>(
        Expression<Func<Vw, bool>>? filter,
        Expression<Func<Vw, TResult>>? selector,
        Expression<Func<Vw, object>>? orderBy,
        bool isDescending,
        CancellationToken cancellationToken,
        params Expression<Func<Vw, object>>[] includes)
    {
        EnsureSelectorOrSameType<TResult>(selector);

        try
        {
            IQueryable<Vw> query = BuildQuery(filter, includes);

            if (orderBy != null)
                query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            return await Project(query, selector).ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            throw new DataAccessException(ex, $"Failed to get projected list for type {typeof(Vw).Name}");
        }
    }

    public virtual async Task<TResult?> GetFirstOrDefaultAsync<TResult>(
        Expression<Func<Vw, bool>> filter,
        Expression<Func<Vw, TResult>> selector,
        CancellationToken cancellationToken = default,
        params Expression<Func<Vw, object>>[] includes)
    {
        if (filter == null) throw new ArgumentNullException(nameof(filter));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        try
        {
            return await BuildQuery(filter, includes)
                .Select(selector)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            throw new DataAccessException(ex, $"Failed to get projected item of type {typeof(Vw).Name}");
        }
    }

    /// <summary>
    /// Views usually have no Id column, so unlike the table repo there is no
    /// default ordering: orderBy is required (SQL Server needs a deterministic
    /// ORDER BY for OFFSET/FETCH paging).
    /// </summary>
    public virtual async Task<PagedResult<TResult>> GetPagedListAsync<TResult>(
        int pageNumber,
        int pageSize,
        Expression<Func<Vw, object>> orderBy,
        Expression<Func<Vw, bool>>? filter = null,
        Expression<Func<Vw, TResult>>? selector = null,
        bool isDescending = false,
        CancellationToken cancellationToken = default,
        params Expression<Func<Vw, object>>[] includes)
    {
        const string methodName = nameof(GetPagedListAsync);

        if (pageNumber < 1)
        {
            logger.LogWarning("Page number must be greater than 0 in {Method}", methodName);
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than 0");
        }

        if (pageSize < 1)
        {
            logger.LogWarning("Page size must be greater than 0 in {Method}", methodName);
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0");
        }

        if (orderBy == null)
        {
            logger.LogWarning("Order by is required for pagination in {Method}", methodName);
            throw new ArgumentNullException(nameof(orderBy), "Order by is required for pagination");
        }

        EnsureSelectorOrSameType<TResult>(selector);

        try
        {
            IQueryable<Vw> query = BuildQuery(filter, includes);

            int totalCount = await query.CountAsync(cancellationToken);

            query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            List<TResult> items = await Project(query, selector).ToListAsync(cancellationToken);

            return new PagedResult<TResult>
            {
                Items = items,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (SqlException ex) when (ex.Message.Contains("Operation cancelled by user"))
        {
            throw;
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            throw new DataAccessException(ex, $"Failed to get paged list for type {typeof(Vw).Name}");
        }
    }

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

    // ---------- Helpers ----------

    private IQueryable<Vw> BuildQuery(
        Expression<Func<Vw, bool>>? filter,
        Expression<Func<Vw, object>>[] includes)
    {
        IQueryable<Vw> query = vwSet.AsNoTracking();

        foreach (var include in includes)
            query = query.Include(include);

        if (filter != null)
            query = query.Where(filter);

        return query;
    }

    // With no selector, TResult must be Vw itself. Query.Cast<TResult>() on an
    // unrelated type would fail at translation time, so we fail early instead.
    private static void EnsureSelectorOrSameType<TResult>(Expression<Func<Vw, TResult>>? selector)
    {
        if (selector == null && typeof(TResult) != typeof(Vw))
            throw new ArgumentException(
                $"A selector is required when {typeof(TResult).Name} differs from {typeof(Vw).Name}.",
                nameof(selector));
    }

    private static IQueryable<TResult> Project<TResult>(
        IQueryable<Vw> query,
        Expression<Func<Vw, TResult>>? selector)
        => selector != null
            ? query.Select(selector)
            : (IQueryable<TResult>)(object)query;
}