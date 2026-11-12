using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
        => await FindAsync<T>(filter, null, null, false, cancellationToken);

    public virtual async Task<IEnumerable<TResult>> FindAsync<TResult>(
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>>? selector,
        Expression<Func<T, object>>? orderBy,
        bool isDescending,
        CancellationToken cancellationToken,
        params Expression<Func<T, object>>[] includes)
    {
        EnsureSelectorOrSameType(selector);

        try
        {
            IQueryable<T> query = BuildQuery(filter, includes);

            if (orderBy != null)
                query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            return await Project(query, selector).ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            throw new DataAccessException(ex, $"Failed to get projected list for type {typeof(T).Name}");
        }
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
        => await GetFirstOrDefaultAsync<T>(filter, null, cancellationToken);

    public virtual async Task<PagedResult<TResult>> GetPagedListAsync<TResult>(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>>? selector,
        Expression<Func<T, object>>? orderBy,
        bool isDescending,
        CancellationToken cancellationToken,
        params Expression<Func<T, object>>[] includes)
    {
        if (pageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than 0");

        if (pageSize < 1)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0");

        orderBy ??= BuildDefaultOrderBy();

        EnsureSelectorOrSameType(selector);

        try
        {
            IQueryable<T> query = BuildQuery(filter, includes);

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
            throw new DataAccessException(ex, $"Failed to get paged list for type {typeof(T).Name}");
        }
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
            throw new DataAccessException(ex, $"Failed to get projected item of type {typeof(T).Name}");
        }
    }

    #region Helpers

    private IQueryable<T> BuildQuery(
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = dbSet.AsNoTracking();

        foreach (var include in includes)
            query = query.Include(include);

        if (filter != null)
            query = query.Where(filter);

        return query;
    }

    // With no selector, TResult must be T itself. Query.Cast<TResult>() on an
    // unrelated type would fail at translation time, so we fail early instead.
    private static Expression<Func<T, object>> BuildDefaultOrderBy()
    {
        var idProperty = typeof(T).GetProperty("Id")
            ?? throw new InvalidOperationException("Order by is required for pagination");

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, idProperty);
        var converted = Expression.Convert(property, typeof(object));

        return Expression.Lambda<Func<T, object>>(converted, parameter);
    }

    private static void EnsureSelectorOrSameType<TResult>(Expression<Func<T, TResult>>? selector)
    {
        if (selector == null && typeof(TResult) != typeof(T))
            throw new ArgumentException(
                $"A selector is required when {typeof(TResult).Name} differs from {typeof(T).Name}.",
                nameof(selector));
    }

    private static IQueryable<TResult> Project<TResult>(
        IQueryable<T> query,
        Expression<Func<T, TResult>>? selector)
        => selector != null
            ? query.Select(selector)
            : (IQueryable<TResult>)(object)query;

    #endregion

}
