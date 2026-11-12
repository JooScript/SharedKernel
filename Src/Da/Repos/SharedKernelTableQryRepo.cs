using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Da.Contracts;
using SharedKernel.Da.Exceptions;
using System.Linq.Expressions;

namespace SharedKernel.Da.Repos;

public class SharedKernelTableQryRepo<Tb, TDbContext>(
    TDbContext context,
    ILogger<SharedKernelTableQryRepo<Tb, TDbContext>> logger) :
    SharedKernelQueryRepo<Tb, TDbContext>(context),
    ISharedKernelTableQryRepo<Tb, TDbContext>
    where Tb : SharedKernelBaseTable
    where TDbContext : DbContext
{
    public virtual async Task<Tb?> GetTrackedOrLoadAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var entity = dbSet.Local.FirstOrDefault(e => e.Id == id);

            if (entity is not null) return entity;

            return await dbSet.Where(a => a.Id == id).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to get entity by ID {id} of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<Tb?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await GetFirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public virtual async Task<TResult?> GetByIdAsync<TResult>(
    Guid id,
    Expression<Func<Tb, TResult>>? selector,
    CancellationToken cancellationToken,
    params Expression<Func<Tb, object>>[] includes)
    {
        try
        {
            IQueryable<Tb> query = dbSet.AsNoTracking();

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

    public virtual async Task<IEnumerable<Tb>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
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
            return await dbSet.Where(e => ids.Distinct().ToList().Contains(e.Id)).AsNoTracking().ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to find multiple entities of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<bool> IsExistsAsync(Guid Id, CancellationToken cancellationToken)
        => await IsExistsAsync(x => x.Id == Id, cancellationToken);

}