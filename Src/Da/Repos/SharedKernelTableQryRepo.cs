using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Da.Contracts;
using SharedKernel.Da.Contracts.Models;
using SharedKernel.Da.Exceptions;
using System.Linq.Expressions;

namespace SharedKernel.Da.Repos;

public class SharedKernelTableQryRepo<Tb, TDbContext>(
    TDbContext context,
    ILogger<SharedKernelTableQryRepo<Tb, TDbContext>> logger)
    : ISharedKernelTableQryRepo<Tb, TDbContext>
    where Tb : SharedKernelBaseTable
    where TDbContext : DbContext
{
    protected readonly DbSet<Tb> tbDbSet = (context ?? throw new ArgumentNullException(nameof(context))).Set<Tb>();

    public async Task<Tb?> GetTrackedOrLoadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = tbDbSet.Local.FirstOrDefault(e => e.Id == id);

            if (entity is not null) return entity;

            return await tbDbSet.Where(a => a.Id == id).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to get entity by ID {id} of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<Tb?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await tbDbSet.Where(a => a.Id == id).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to get entity by ID {id} of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<TResult?> FindAsync<TResult>(
    Guid id,
    Expression<Func<Tb, TResult>> selector,
    CancellationToken cancellationToken = default,
    params Expression<Func<Tb, object>>[] includes)
    {
        try
        {
            IQueryable<Tb> query = tbDbSet.AsNoTracking();

            foreach (var include in includes)
                query = query.Include(include);

            return await query
                .Where(x => x.Id == id)
                .Select(selector)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(
                ex,
                $"Failed to get entity by ID {id} of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<IEnumerable<Tb>> FindAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids == null)
        {
            logger.LogWarning($"Ids collection cannot be null in {nameof(FindAsync)}");
            throw new ArgumentNullException(nameof(ids));
        }

        if (!ids.Any())
        {
            return Enumerable.Empty<Tb>();
        }

        try
        {
            return await tbDbSet.Where(e => ids.Distinct().ToList().Contains(e.Id)).AsNoTracking().ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to find multiple entities of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<IEnumerable<Tb>> FindAsync(Expression<Func<Tb, bool>> predicate, CancellationToken cancellationToken = default)
    {
        if (predicate == null)
        {
            logger.LogWarning($"Predicate cannot be null in {nameof(FindAsync)}");
            throw new ArgumentNullException(nameof(predicate));
        }

        try
        {
            return await tbDbSet.Where(predicate).AsNoTracking().ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to find entities using predicate for type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<PagedResult<TResult>> GetPagedListAsync<TResult>(
    int pageNumber,
    int pageSize,
    Expression<Func<Tb, bool>>? filter = null,
    Expression<Func<Tb, TResult>>? selector = null,
    Expression<Func<Tb, object>>? orderBy = null,
    bool isDescending = false,
    CancellationToken cancellationToken = default,
    params Expression<Func<Tb, object>>[] includes)
    {
        const string methodName = nameof(GetPagedListAsync);

        if (pageNumber < 1)
        {
            logger.LogWarning($"Page number must be greater than 0 in {methodName}");
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than 0");
        }

        if (pageSize < 1)
        {
            logger.LogWarning($"Page size must be greater than 0 in {methodName}");
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than 0");
        }

        try
        {
            IQueryable<Tb> query = tbDbSet.AsQueryable();

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            int totalCount = await query.CountAsync(cancellationToken);

            if (orderBy != null)
            {
                query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);
            }
            else
            {
                var idProperty = typeof(Tb).GetProperty("Id");

                if (idProperty != null)
                {
                    var parameter = Expression.Parameter(typeof(Tb), "x");
                    var property = Expression.Property(parameter, idProperty);
                    var lambda = Expression.Lambda<Func<Tb, object>>(Expression.Convert(property, typeof(object)), parameter);

                    query = isDescending ?
                        query.OrderByDescending(lambda) :
                        query.OrderBy(lambda);

                }
                else
                {
                    // Log warning but still order by something?
                    // Or throw exception requiring order by?
                    logger.LogError("No order by specified and no Id property found. Using first property as order by.");
                    // This is risky - better to throw
                    throw new InvalidOperationException("Order by is required for pagination");
                }
            }

            query = query.AsNoTracking().Skip((pageNumber - 1) * pageSize).Take(pageSize);

            List<TResult> items = selector != null
                ? await query.Select(selector).ToListAsync(cancellationToken)
                : await query.Cast<TResult>().ToListAsync(cancellationToken);

            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResult<TResult>
            {
                Items = items,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
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
        catch (Exception ex)
        {
            throw new DataAccessException(ex,
                $"Failed to get paged list for type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<List<Tb>> GetListAsync(Expression<Func<Tb, bool>> filter, CancellationToken cancellationToken = default)
    {
        try
        {
            return await tbDbSet.Where(filter).AsNoTracking().ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to get filtered list for type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<List<TResult>> GetListAsync<TResult>(Expression<Func<Tb, bool>>? filter = null, Expression<Func<Tb, TResult>>? selector = null, Expression<Func<Tb, object>>? orderBy = null, bool isDescending = false, CancellationToken cancellationToken = default, params Expression<Func<Tb, object>>[] includes)
    {
        try
        {
            IQueryable<Tb> query = tbDbSet.AsQueryable();

            foreach (var include in includes)
                query = query.Include(include);

            if (filter != null)
                query = query.Where(filter);

            if (orderBy != null)
                query = isDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            query = query.AsNoTracking();

            List<TResult> items;

            if (selector != null)
                items = await query.Select(selector).ToListAsync(cancellationToken);
            else
                items = await query.Cast<TResult>().ToListAsync(cancellationToken);

            return items;
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to get projected list for type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<List<Tb>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await tbDbSet.Where(a => a.CurrentState > 0).AsNoTracking().ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to get all entities of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<Tb?> GetFirstOrDefaultAsync(Expression<Func<Tb, bool>> filter, CancellationToken cancellationToken = default)
    {
        try
        {
            return await tbDbSet.Where(filter).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed in GetFirstOrDefault for type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<int> CountAsync(Expression<Func<Tb, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            IQueryable<Tb> query = tbDbSet.AsQueryable();

            if (filter != null)
                query = query.Where(filter);

            return await query.CountAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to count entities of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<bool> IsExistsAsync(Expression<Func<Tb, bool>> filter, CancellationToken cancellationToken = default)
    {
        try
        {
            return await tbDbSet.AnyAsync(filter, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed in existence check for type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<bool> IsExistsAsync(Guid Id, CancellationToken cancellationToken = default) => await IsExistsAsync(x => x.Id == Id, cancellationToken);

}