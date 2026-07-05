using Microsoft.EntityFrameworkCore;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Contracts.Base;
using SharedKernel.Bl.Contracts.Events;
using SharedKernel.Bl.Events.Args;
using SharedKernel.Bl.Exceptions;
using SharedKernel.Bl.Models;
using SharedKernel.Da.Contracts;
using SharedKernel.Domains.Types;
using System.Linq.Expressions;
using static SharedKernel.Bl.Enums.ServicesEnums;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Bl.Services.Base;

public abstract class SharedKernelBaseService<Tb, Dto, TDbContext>
    : IBaseService<Tb, Dto>
    where Tb : SharedKernelBaseTable
    where Dto : class
    where TDbContext : DbContext
{
    readonly ISharedKernelTableQryRepo<Tb, TDbContext> _repoQry;
    readonly ISharedKernelTableCmdRepo<Tb, TDbContext> _repoCmd;
    readonly IUserQry _userServiceQuery;
    readonly ISharedKernelUnitOfWork<TDbContext>? _UnitOfWork;
    private readonly ICrudPublisher _publisher;

    public SharedKernelBaseService(ISharedKernelTableQryRepo<Tb, TDbContext> repoQry, ISharedKernelTableCmdRepo<Tb, TDbContext> repoCmd, IUserQry userQry, ICrudPublisher publisher)
    {
        _repoQry = repoQry;
        _repoCmd = repoCmd;
        _userServiceQuery = userQry;
        _publisher = publisher;
    }

    public SharedKernelBaseService(ISharedKernelUnitOfWork<TDbContext> unitOfWork, ISharedKernelTableQryRepo<Tb, TDbContext> repoQry, IUserQry userServiceQry, ICrudPublisher publisher)
    {
        _UnitOfWork = unitOfWork;
        _repoQry = repoQry;
        _repoCmd = unitOfWork.Repository<Tb>();
        _userServiceQuery = userServiceQry;
        _publisher = publisher;
    }

    public abstract Dto ToDto(Tb entity);

    public abstract Tb ToEntity(Dto dto);

    public virtual async Task<List<Dto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _repoQry.GetAllAsync(cancellationToken);
        return entities.Select(ToDto).ToList();
    }

    public virtual async Task<Dto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repoQry.FindAsync(id);
        return entity == null ? null : ToDto(entity);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken)
        => await _repoQry.CountAsync(e => e.CurrentState > 0, cancellationToken);

    public virtual async Task<bool> IsExistsAsync(Expression<Func<Tb, bool>> filter, CancellationToken cancellationToken = default)
        => await _repoQry.IsExistsAsync(filter, cancellationToken);

    public virtual async Task<bool> IsExistsAsync(Guid Id, CancellationToken cancellationToken)
        => await _repoQry.IsExistsAsync(Id, cancellationToken);

    public virtual async Task<EntityValidationResult> CanDeleteAsync(Guid id)
    {
        return new(true, string.Empty);
    }

    public virtual async Task<EntityValidationResult> CanUpdateAsync(Dto entity)
    {
        return new(true, string.Empty);
    }

    #region Cmd

    public virtual async Task<SaveResultType> AddAsync(Dto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var dbObject = ToEntity(dto);

            if (dbObject is null)
                throw new Exception("The Database Object Cannot be null");

            var result = await _repoCmd.AddAsync(BlHelper.NormalizeDbObjToAdd(dbObject, _userServiceQuery.GetLoggedInUserId()));

            return result;
        }
        catch (Exception ex)
        {
            throw new BusinessLogicException(ex, "Error While Adding Entity");
        }
    }

    public async virtual Task<bool> UpdateAsync(Dto entity, CancellationToken cancellationToken = default)
    {
        var check = await CanUpdateAsync(entity);

        if (!check.Success)
            return false;

        return await _repoCmd.UpdateAsync(BlHelper.NormalizeDbObjToUpdate(ToEntity(entity), _userServiceQuery.GetLoggedInUserId()));
    }

    public virtual async Task<bool> ChangeStatusAsync(Guid id, CurrentStateType status = CurrentStateType.Active, CancellationToken cancellationToken = default)
    {
        Tb? entity = await _repoQry.FindAsync(id);

        if (entity == null) return false;

        CurrentStateType previousState = entity.CurrentState;

        entity.CurrentState = status;
        Guid userId = _userServiceQuery.GetLoggedInUserId();
        entity.UpdatedBy = userId;

        bool success = await _repoCmd.UpdateAsync(entity);

        return success;
    }

    public virtual async Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken = default) => await ChangeStatusAsync(id, CurrentStateType.Active, cancellationToken);

    public virtual async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken = default) => await ChangeStatusAsync(id, CurrentStateType.InActive, cancellationToken);

    public virtual async Task<bool> DeleteAsync(Guid id, DeleteType deleteType = DeleteType.HardDelete, CancellationToken cancellationToken = default)
    {
        var check = await CanDeleteAsync(id);

        if (!check.Success)
        {
            return false;
        }

        if (deleteType == DeleteType.SoftDelete)
        {
            return await ChangeStatusAsync(id, CurrentStateType.Deleted, cancellationToken);
        }
        else
        {
            return await _repoCmd.HardDeleteAsync(id);
        }
    }

    public virtual async Task<bool> DeleteAsync(List<Guid> Ids, DeleteType deleteType, CancellationToken cancellationToken = default)
    {
        bool succeeded = true;

        foreach (var id in Ids)
        {
            succeeded &= await DeleteAsync(id, deleteType, cancellationToken);
        }

        return succeeded;
    }

    #endregion

}