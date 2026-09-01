using Microsoft.EntityFrameworkCore;
using SharedKernel.Bl.Contracts.Auth;
using SharedKernel.Bl.Contracts.Base;
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
    readonly ISharedKernelTableQryRepo<Tb, TDbContext> repoQry;
    readonly ISharedKernelTableCmdRepo<Tb, TDbContext> repoCmd;
    readonly IUserQry userQry;
    readonly ISharedKernelUnitOfWork<TDbContext>? _UnitOfWork;

    public SharedKernelBaseService(ISharedKernelTableQryRepo<Tb, TDbContext> repoQry, ISharedKernelTableCmdRepo<Tb, TDbContext> repoCmd, IUserQry userQry)
    {
        this.repoQry = repoQry;
        this.repoCmd = repoCmd;
        this.userQry = userQry;
    }

    public SharedKernelBaseService(ISharedKernelUnitOfWork<TDbContext> unitOfWork, ISharedKernelTableQryRepo<Tb, TDbContext> repoQry, IUserQry userQry)
    {
        _UnitOfWork = unitOfWork;
        this.repoQry = repoQry;
        repoCmd = unitOfWork.Repository<Tb>();
        this.userQry = userQry;
    }

    public abstract Dto ToDto(Tb entity);

    public abstract Tb ToEntity(Dto dto);

    public virtual async Task<List<Dto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await repoQry.GetAllAsync(cancellationToken);
        return entities.Select(ToDto).ToList();
    }

    public virtual async Task<Dto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await repoQry.FindAsync(id);
        return entity == null ? null : ToDto(entity);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken)
        => await repoQry.CountAsync(e => e.CurrentState > 0, cancellationToken);

    public virtual async Task<bool> IsExistsAsync(Expression<Func<Tb, bool>> filter, CancellationToken cancellationToken = default)
        => await repoQry.IsExistsAsync(filter, cancellationToken);

    public virtual async Task<bool> IsExistsAsync(Guid Id, CancellationToken cancellationToken)
        => await repoQry.IsExistsAsync(Id, cancellationToken);

    public virtual async Task<EntityValidationResult> CanDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return new(true, string.Empty);
    }

    public virtual async Task<EntityValidationResult> CanUpdateAsync(Dto dto, CancellationToken cancellationToken = default)
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

            var result = await repoCmd.AddAsync(BlHelper.NormalizeDbObjToAdd(dbObject, userQry.LoggedInUserId));

            return result;
        }
        catch (Exception ex)
        {
            throw new BusinessLogicException(ex, "Error While Adding Entity");
        }
    }

    public async virtual Task<bool> UpdateAsync(Dto dto, CancellationToken cancellationToken = default)
    {
        var check = await CanUpdateAsync(dto);

        if (!check.Success)
            return false;

        return await repoCmd.UpdateAsync(BlHelper.NormalizeDbObjToUpdate(ToEntity(dto), userQry.LoggedInUserId));
    }

    public virtual async Task<bool> ChangeStatusAsync(Guid id, CurrentStateType status = CurrentStateType.Active, CancellationToken cancellationToken = default)
    {
        Tb? entity = await repoQry.FindAsync(id);

        if (entity == null) return false;

        CurrentStateType previousState = entity.CurrentState;

        entity.CurrentState = status;
        Guid userId = userQry.LoggedInUserId;
        entity.UpdatedBy = userId;

        bool success = await repoCmd.UpdateAsync(entity);

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
            return await repoCmd.HardDeleteAsync(id);
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