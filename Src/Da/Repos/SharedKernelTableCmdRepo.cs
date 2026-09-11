using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedKernel.Da.Contracts;
using SharedKernel.Da.Exceptions;
using SharedKernel.Domains.Types;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Da.Repos;

public class SharedKernelTableCmdRepo<Tb, TDbContext>(
    TDbContext context,
    ILogger<SharedKernelTableCmdRepo<Tb, TDbContext>> logger,
    ISharedKernelTableQryRepo<Tb, TDbContext> repoQry)
    : ISharedKernelTableCmdRepo<Tb, TDbContext>
    where Tb : SharedKernelBaseTable
    where TDbContext : DbContext
{
    protected readonly DbSet<Tb> tbDbSet = (context ?? throw new ArgumentNullException(nameof(context))).Set<Tb>();

    public virtual async Task<SaveResultType> AddAsync(Tb entity, CancellationToken cancellationToken = default)
    {
        const string methodName = nameof(AddAsync);

        if (entity == null)
        {
            logger.LogWarning($"Entity cannot be null in {methodName}");
            throw new ArgumentNullException(nameof(entity));
        }

        try
        {
            entity.Id = Guid.Empty;
            entity.CreatedAt = DateTime.UtcNow;
            await tbDbSet.AddAsync(entity, cancellationToken);

            return SaveResultType.From(await context.SaveChangesAsync(cancellationToken) > 0, entity.Id);
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to add entity of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<bool> AddAsync(IEnumerable<Tb> entities, CancellationToken cancellationToken = default)
    {
        const string methodName = nameof(AddAsync);

        if (entities == null)
        {
            logger.LogWarning($"Entities collection cannot be null in {methodName}");
            throw new ArgumentNullException(nameof(entities));
        }

        if (!entities.Any())
            return true;

        try
        {
            var utcNow = DateTime.UtcNow;
            var entityList = entities.ToList();

            foreach (var entity in entityList)
            {
                entity.Id = Guid.Empty;
                entity.CreatedAt = utcNow;
            }

            await tbDbSet.AddRangeAsync(entityList, cancellationToken);

            return await context.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to add entities of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<bool> UpdateAsync(Tb entity, CancellationToken cancellationToken = default)
    {
        const string methodName = nameof(UpdateAsync);
        string _loggerMsg = $"Warning in {methodName} In Table Cmd Repo of {typeof(Tb)}";

        if (entity == null)
        {
            logger.LogWarning(_loggerMsg, "Entity cannot be null");
            throw new ArgumentNullException(nameof(entity));
        }

        try
        {
            var dbData = await repoQry.GetTrackedOrLoadAsync(entity.Id, cancellationToken);

            if (dbData is null)
            {
                logger.LogWarning($"There is no {typeof(Tb).Name} entity with id {entity.Id}");
                return false;
            }

            entity.UpdatedAt = DateTimeOffset.UtcNow;

            var entry = context.Entry(dbData);

            entry.CurrentValues.SetValues(entity);
            entry.Property(x => x.CreatedAt).IsModified = false;
            entry.Property(x => x.CreatedBy).IsModified = false;

            return await context.SaveChangesAsync(cancellationToken) > 0;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new DataAccessException(ex, $"Concurrency conflict updating entity {entity.Id}");
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to update entity {entity.Id} of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<bool> UpdateAsync(IEnumerable<Tb> entities, CancellationToken cancellationToken = default)
    {
        const string methodName = nameof(UpdateAsync);

        if (entities == null)
        {
            logger.LogError($"Error in {methodName}: Entities collection cannot be null");
            throw new ArgumentNullException(nameof(entities));
        }

        var entityList = entities.ToList();
        if (!entityList.Any())
            return true;

        var ids = entityList.Select(e => e.Id).ToList();
        var utcNow = DateTime.UtcNow;

        try
        {
            var trackedEntities = context.Set<Tb>()
                .Local
                .Where(e => ids.Contains(e.Id))
                .ToDictionary(e => e.Id);

            var idsToFetch = ids.Except(trackedEntities.Keys).ToList();

            if (idsToFetch.Any())
            {
                var dbEntities = await repoQry.GetByIdsAsync(idsToFetch, cancellationToken);

                foreach (var dbEntity in dbEntities)
                {
                    trackedEntities[dbEntity.Id] = dbEntity;
                }
            }

            foreach (var entity in entityList)
            {
                if (!trackedEntities.TryGetValue(entity.Id, out var dbData))
                {
                    logger.LogError($"Error In {methodName}: Failed to update");
                    return false;
                }

                entity.UpdatedAt = utcNow;

                var entry = context.Entry(dbData);
                entry.CurrentValues.SetValues(entity);
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Property(x => x.CreatedBy).IsModified = false;
            }

            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new DataAccessException(ex, "Concurrency conflict updating entities");
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to update entities of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<bool> ChangeStatusAsync(Guid id, Guid userId, CurrentStateType status = CurrentStateType.Active, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await repoQry.GetTrackedOrLoadAsync(id, cancellationToken);

            if (entity == null)
                return false;

            entity.CurrentState = status;
            entity.UpdatedBy = userId;
            entity.UpdatedAt = DateTime.UtcNow;

            context.Entry(entity).State = EntityState.Modified;
            await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to change status for entity {id} of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<bool> ChangeStatusAsync(IEnumerable<Guid> ids, Guid userId, CurrentStateType status = CurrentStateType.Active, CancellationToken cancellationToken = default)
    {
        const string methodName = nameof(ChangeStatusAsync);

        if (ids == null)
        {
            logger.LogWarning($"Ids collection cannot be null in {methodName}");
            throw new ArgumentNullException(nameof(ids));
        }

        if (!ids.Any())
        {
            return true; // Consider empty collection as successful
        }

        try
        {
            var distinctIds = ids.Distinct().ToList();
            var entities = await tbDbSet
                .Where(e => distinctIds.Contains(e.Id))
                .ToListAsync(cancellationToken);

            // Check if all entities were found
            if (entities.Count != distinctIds.Count)
            {
                var foundIds = entities.Select(e => e.Id).ToList();
                var missingIds = distinctIds.Except(foundIds).ToList();

                logger.LogWarning($"Some entities not found: {string.Join(", ", missingIds)} in {methodName}");
                return false;
            }

            var utcNow = DateTime.UtcNow;
            foreach (var entity in entities)
            {
                entity.CurrentState = status;
                entity.UpdatedBy = userId;
                entity.UpdatedAt = utcNow;
                context.Entry(entity).State = EntityState.Modified;
            }

            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex,
                $"Failed to change status for multiple entities of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<bool> HardDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await repoQry.GetByIdAsync(id, cancellationToken);

            if (entity == null)
                return false;

            context.Entry(entity).State = EntityState.Deleted;
            await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to hard delete entity {id} of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<bool> HardDeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        const string methodName = nameof(ChangeStatusAsync);

        if (ids == null)
        {
            logger.LogWarning($"Ids collection cannot be null in {methodName}");
            throw new ArgumentNullException(nameof(ids));
        }

        if (!ids.Any())
        {
            return true;
        }

        try
        {
            var distinctIds = ids.Distinct().ToList();
            var entities = await tbDbSet
                .Where(e => distinctIds.Contains(e.Id))
                .ToListAsync(cancellationToken);

            // Check if all entities were found
            if (entities.Count != distinctIds.Count)
            {
                var foundIds = entities.Select(e => e.Id).ToList();
                var missingIds = distinctIds.Except(foundIds).ToList();

                logger.LogWarning($"Some entities not found: {string.Join(", ", missingIds)} in {methodName}");
                return false;
            }

            foreach (var entity in entities)
            {
                context.Entry(entity).State = EntityState.Deleted;
            }

            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex,
                $"Failed to hard delete for multiple entities of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<bool> HardDeleteAsync(Tb entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
            {
                return false;
            }

            context.Entry(entity).State = EntityState.Deleted;
            await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            throw new DataAccessException(ex, $"Failed to hard delete entity {entity.Id} of type {typeof(Tb).Name}");
        }
    }

    public virtual async Task<bool> HardDeleteAsync(IEnumerable<Tb> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entities == null)
            {
                return false;
            }

            foreach (var entity in entities)
            {
                context.Entry(entity).State = EntityState.Deleted;
            }

            await context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            foreach (var entity in entities)
            {
                throw new DataAccessException(ex, $"Failed to hard delete entity {entity.Id} of type {typeof(Tb).Name}");
            }

            return false;
        }
    }

    public virtual async Task<bool> SoftDeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await ChangeStatusAsync(id, userId, (int)CurrentStateType.Deleted, cancellationToken);
    }

    public virtual async Task<bool> SoftDeleteAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken cancellationToken = default)
    {
        return await ChangeStatusAsync(ids, userId, (int)CurrentStateType.Deleted, cancellationToken);
    }

}