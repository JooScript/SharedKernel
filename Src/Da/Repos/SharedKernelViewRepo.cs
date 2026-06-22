using Microsoft.EntityFrameworkCore;
using SharedKernel.Da.Contracts;
using SharedKernel.Da.Exceptions;
using System.Linq.Expressions;

namespace SharedKernel.Da.Repos;

public class SharedKernelViewRepo<Vw, TDbContext>(TDbContext context) : ISharedKernelViewRepo<Vw>
    where Vw : class
    where TDbContext : DbContext
{
    protected readonly DbSet<Vw> dbSet = (context ?? throw new ArgumentNullException(nameof(context))).Set<Vw>();

    public async Task<IReadOnlyList<Vw>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbSet.AsNoTracking().ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            throw new DataAccessException(ex, $"Failed to retrieve all items of type {typeof(Vw).Name}");
        }
    }

    public async Task<Vw?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbSet.FindAsync(new object[] { id }, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to retrieve item of type {typeof(Vw).Name} with ID {id}");
        }
    }

    public async Task<Vw?> GetFirstOrDefaultAsync(Expression<Func<Vw, bool>> filter, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbSet.Where(filter).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to retrieve first matching item of type {typeof(Vw).Name}");
        }
    }

    public async Task<IReadOnlyList<Vw>> GetListAsync(Expression<Func<Vw, bool>> filter, CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbSet.Where(filter).AsNoTracking().ToListAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not DataAccessException)
        {
            throw new DataAccessException(ex, $"Failed to retrieve filtered list of type {typeof(Vw).Name}");
        }
    }

}