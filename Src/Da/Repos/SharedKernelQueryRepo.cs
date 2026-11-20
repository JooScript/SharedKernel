using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Da.Contracts;
using SharedKernel.Da.Contracts.Models;
using SharedKernel.Da.Exceptions;
using System.Linq.Expressions;

namespace SharedKernel.Da.Repos;

public class SharedKernelQueryRepo<T, TDbContext>(
    TDbContext context,
    ILogger<SharedKernelQueryRepo<T, TDbContext>> logger) :
    ISharedKernelQueryRepo<T, TDbContext>
    where T : class
    where TDbContext : DbContext
{
    protected readonly DbSet<T> dbSet = (context ?? throw new ArgumentNullException(nameof(context))).Set<T>();

    public virtual async Task<IEnumerable<T>?> GetTrackedOrLoadAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken)
    {
        try
        {
            var entity = dbSet.Local.AsEnumerable().Where(filter.Compile());

            if (entity.Any()) return entity;

            return await dbSet.Where(filter).ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to get entity of type {typeof(T).Name}");
        }
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? filter, CancellationToken cancellationToken)
    {
        const string methodName = nameof(CountAsync);
        string entityType = typeof(T).Name;

        logger.LogDebug(
            "{Method}: Counting {EntityType} records. Filter applied: {HasFilter}",
            methodName,
            entityType,
            filter is not null);

        try
        {
            IQueryable<T> query = dbSet.AsQueryable();

            if (filter != null)
                query = query.Where(filter);

            int count = await query.CountAsync(cancellationToken);

            logger.LogDebug(
                "{Method}: Counted {Count} {EntityType} records",
                methodName,
                count,
                entityType);

            return count;
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            logger.LogError(
                ex,
                "{Method}: Failed to count {EntityType} records",
                methodName,
                entityType);

            throw new DataAccessException(ex, $"Failed to count items of type {entityType}");
        }
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>>? filter, CancellationToken cancellationToken)
        => await FindAsync<T>(filter, null, null, false, cancellationToken);

    public virtual async Task<IEnumerable<TResult>> FindAsync<TResult>(
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>>? selector,
        Expression<Func<T, object>>? orderBy,
        bool isDescending,
        CancellationToken cancellationToken,
        params Expression<Func<T, object>>[] includes)
    {
        const string methodName = nameof(FindAsync);
        string entityType = typeof(T).Name;
        string resultType = typeof(TResult).Name;

        EnsureSelectorOrSameType(selector);

        logger.LogDebug(
            "{Method}: Querying {EntityType} records as {ResultType}. Filter applied: {HasFilter}, Ordered: {IsOrdered}, Descending: {IsDescending}, Includes: {IncludeCount}",
            methodName,
            entityType,
            resultType,
            filter is not null,
            orderBy is not null,
            isDescending,
            includes.Length);

        try
        {
            IQueryable<T> query = BuildQuery(filter, includes);

            if (orderBy != null)
                query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            List<TResult> results = await Project(query, selector).ToListAsync(cancellationToken);

            logger.LogDebug(
                "{Method}: Retrieved {Count} {ResultType} records from {EntityType}",
                methodName,
                results.Count,
                resultType,
                entityType);

            return results;
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            logger.LogError(
                ex,
                "{Method}: Failed to get projected list of {ResultType} from {EntityType}",
                methodName,
                resultType,
                entityType);

            throw new DataAccessException(ex, $"Failed to get projected list for type {entityType}");
        }
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string methodName = nameof(GetAllAsync);
        string entityType = typeof(T).Name;

        logger.LogDebug(
            "{Method}: Retrieving all {EntityType} records",
            methodName,
            entityType);

        try
        {
            List<T> items = await dbSet.AsNoTracking().ToListAsync(cancellationToken);

            logger.LogDebug(
                "{Method}: Retrieved {Count} {EntityType} records",
                methodName,
                items.Count,
                entityType);

            return items;
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            logger.LogError(
                ex,
                "{Method}: Failed to retrieve all {EntityType} records",
                methodName,
                entityType);

            throw new DataAccessException(ex, $"Failed to retrieve all items of type {entityType}");
        }
    }

    public virtual async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>>? filter, CancellationToken cancellationToken)
        => await GetFirstOrDefaultAsync<T>(filter, x => x, cancellationToken);

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
        const string methodName = nameof(GetPagedListAsync);
        string entityType = typeof(T).Name;
        string resultType = typeof(TResult).Name;

        if (pageNumber < 1)
        {
            logger.LogWarning(
                "{Method}: Invalid page number {PageNumber} for {EntityType}",
                methodName,
                pageNumber,
                entityType);

            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than 0");
        }

        if (pageSize < 1)
        {
            logger.LogWarning(
                "{Method}: Invalid page size {PageSize} for {EntityType}",
                methodName,
                pageSize,
                entityType);

            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0");
        }

        orderBy ??= BuildDefaultOrderBy();

        EnsureSelectorOrSameType(selector);

        logger.LogDebug(
            "{Method}: Retrieving page {PageNumber} with size {PageSize} of {EntityType} records as {ResultType}. Filter applied: {HasFilter}, Descending: {IsDescending}, Includes: {IncludeCount}",
            methodName,
            pageNumber,
            pageSize,
            entityType,
            resultType,
            filter is not null,
            isDescending,
            includes.Length);

        try
        {
            IQueryable<T> query = BuildQuery(filter, includes);

            int totalCount = await query.CountAsync(cancellationToken);

            query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

            List<TResult> items = await Project(query, selector).ToListAsync(cancellationToken);

            logger.LogDebug(
                "{Method}: Retrieved {ItemCount} of {TotalCount} {ResultType} records from {EntityType} for page {PageNumber}",
                methodName,
                items.Count,
                totalCount,
                resultType,
                entityType,
                pageNumber);

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
            logger.LogDebug(
                "{Method}: Retrieving page {PageNumber} of {EntityType} records was cancelled",
                methodName,
                pageNumber,
                entityType);

            throw;
        }
        catch (SqlException ex) when (ex.Message.Contains("Operation cancelled by user"))
        {
            logger.LogDebug(
                ex,
                "{Method}: Retrieving page {PageNumber} of {EntityType} records was cancelled by the database",
                methodName,
                pageNumber,
                entityType);

            throw;
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            logger.LogError(
                ex,
                "{Method}: Failed to get page {PageNumber} of {ResultType} records from {EntityType}",
                methodName,
                pageNumber,
                resultType,
                entityType);

            throw new DataAccessException(ex, $"Failed to get paged list for type {entityType}");
        }
    }

    public virtual async Task<bool> IsExistsAsync(Expression<Func<T, bool>>? filter, CancellationToken cancellationToken)
    {
        const string methodName = nameof(IsExistsAsync);
        string entityType = typeof(T).Name;

        if (filter == null)
        {
            logger.LogWarning(
                "{Method}: Filter cannot be null for {EntityType}",
                methodName,
                entityType);

            throw new ArgumentNullException(nameof(filter));
        }

        logger.LogDebug(
            "{Method}: Checking existence of {EntityType} records",
            methodName,
            entityType);

        try
        {
            bool exists = await dbSet.AnyAsync(filter, cancellationToken);

            logger.LogDebug(
                "{Method}: Existence check for {EntityType} returned {Exists}",
                methodName,
                entityType,
                exists);

            return exists;
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            logger.LogError(
                ex,
                "{Method}: Failed in existence check for {EntityType}",
                methodName,
                entityType);

            throw new DataAccessException(ex, $"Failed in existence check for type {entityType}");
        }
    }

    public virtual async Task<TResult?> GetFirstOrDefaultAsync<TResult>(
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken,
        params Expression<Func<T, object>>[] includes)
    {
        const string methodName = nameof(GetFirstOrDefaultAsync);
        string entityType = typeof(T).Name;
        string resultType = typeof(TResult).Name;

        if (filter == null)
        {
            logger.LogWarning(
                "{Method}: Filter cannot be null for {EntityType}",
                methodName,
                entityType);

            throw new ArgumentNullException(nameof(filter));
        }

        if (selector == null)
        {
            logger.LogWarning(
                "{Method}: Selector cannot be null for {EntityType}",
                methodName,
                entityType);

            throw new ArgumentNullException(nameof(selector));
        }

        logger.LogDebug(
            "{Method}: Retrieving first {ResultType} record from {EntityType}. Includes: {IncludeCount}",
            methodName,
            resultType,
            entityType,
            includes.Length);

        try
        {
            TResult? result = await BuildQuery(filter, includes)
                .Select(selector)
                .FirstOrDefaultAsync(cancellationToken);

            logger.LogDebug(
                "{Method}: First {ResultType} record from {EntityType} was {ResultState}",
                methodName,
                resultType,
                entityType,
                result is null ? "not found" : "found");

            return result;
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            logger.LogError(
                ex,
                "{Method}: Failed to get first {ResultType} record from {EntityType}",
                methodName,
                resultType,
                entityType);

            throw new DataAccessException(ex, $"Failed to get projected item of type {entityType}");
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
