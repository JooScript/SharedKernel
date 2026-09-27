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
    #region Private fields

    readonly ISharedKernelTableQryRepo<Tb, TDbContext> qryRepo;
    readonly ISharedKernelTableCmdRepo<Tb, TDbContext> cmdRepo;
    readonly IUserQry userQry;

    #endregion

    public SharedKernelBaseService(
        ISharedKernelTableCmdRepo<Tb, TDbContext> cmdRepo,
        ISharedKernelTableQryRepo<Tb, TDbContext> qryRepo,
        IUserQry userQry)
    {
        this.qryRepo = qryRepo;
        this.cmdRepo = cmdRepo;
        this.userQry = userQry;
    }

    public abstract Dto ToDto(Tb entity);

    public abstract Tb ToEntity(Dto dto);

    public virtual async Task<List<Dto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await qryRepo.GetAllAsync(cancellationToken);
        return entities.Select(ToDto).ToList();
    }

    public virtual async Task<Dto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await qryRepo.GetByIdAsync(id, cancellationToken);
        return entity == null ? null : ToDto(entity);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken)
        => await qryRepo.CountAsync(e => e.CurrentState > 0, cancellationToken);

    public virtual async Task<bool> IsExistsAsync(Expression<Func<Tb, bool>> filter, CancellationToken cancellationToken)
        => await qryRepo.IsExistsAsync(filter, cancellationToken);

    public virtual async Task<bool> IsExistsAsync(Guid Id, CancellationToken cancellationToken)
        => await qryRepo.IsExistsAsync(Id, cancellationToken);

    public virtual async Task<EntityValidationResult> CanDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return new(true, string.Empty);
    }

    public virtual async Task<EntityValidationResult> CanUpdateAsync(Dto dto, CancellationToken cancellationToken)
    {
        return new(true, string.Empty);
    }

    #region Cmd

    public virtual async Task<SaveResultType> AddAsync(Dto dto, CancellationToken cancellationToken)
    {
        try
        {
            var dbObject = ToEntity(dto);

            if (dbObject is null)
                throw new Exception("The Database Object Cannot be null");

            var result = await cmdRepo.AddAsync(
                BlHelper.NormalizeDbObjToAdd(dbObject, userQry.LoggedInUserId),
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            throw new BusinessLogicException(ex, "Error While Adding Entity");
        }
    }

    public async virtual Task<bool> UpdateAsync(Dto dto, CancellationToken cancellationToken)
    {
        var check = await CanUpdateAsync(dto, cancellationToken);

        if (!check.Success)
            return false;

        return await cmdRepo.UpdateAsync(BlHelper.NormalizeDbObjToUpdate(
            ToEntity(dto),
            userQry.LoggedInUserId), cancellationToken);
    }

    public virtual async Task<bool> ChangeStatusAsync(Guid id, CurrentStateType status, CancellationToken cancellationToken)
    {
        Tb? entity = await qryRepo.GetByIdAsync(id, cancellationToken);

        if (entity == null) return false;

        CurrentStateType previousState = entity.CurrentState;

        entity.CurrentState = status;
        Guid userId = userQry.LoggedInUserId;
        entity.UpdatedBy = userId;

        bool success = await cmdRepo.UpdateAsync(entity, cancellationToken);

        return success;
    }

    public virtual async Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken) => await ChangeStatusAsync(id, CurrentStateType.Active, cancellationToken);

    public virtual async Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken) => await ChangeStatusAsync(id, CurrentStateType.InActive, cancellationToken);

    public virtual async Task<bool> DeleteAsync(Guid id, DeleteType deleteType, CancellationToken cancellationToken)
    {
        if (!(await CanDeleteAsync(id, cancellationToken)).Success)
            return false;

        if (deleteType == DeleteType.SoftDelete)
            return await ChangeStatusAsync(id, CurrentStateType.Deleted, cancellationToken);
        else
            return await cmdRepo.HardDeleteAsync(id, cancellationToken);
    }

    public virtual async Task<bool> DeleteAsync(List<Guid> Ids, DeleteType deleteType, CancellationToken cancellationToken)
    {
        bool succeeded = true;

        foreach (var id in Ids)
            succeeded &= await DeleteAsync(id, deleteType, cancellationToken);

        return succeeded;
    }

    #endregion

}