using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using SharedKernel.Da.Exceptions;
using SharedKernel.Domains.Contracts.Generic;
using SharedKernel.Core.Exceptions;

namespace SharedKernel.Da.Repos;

public class ViewRepo<Vw>(DbContext context) : IViewRepo<Vw> where Vw : class
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

    public async Task<Vw> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await dbSet.FindAsync(new object[] { id }, cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException(typeof(Vw).Name, id);
            }

            return entity;
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to retrieve item of type {typeof(Vw).Name} with ID {id}");
        }
    }

    public async Task<Vw> GetFirstOrDefaultAsync(Expression<Func<Vw, bool>> filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await dbSet.Where(filter).AsNoTracking().FirstOrDefaultAsync(cancellationToken);

            if (entity == null)
            {
                throw new NotFoundException();
            }

            return entity;
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