using Microsoft.EntityFrameworkCore;
using SharedKernel.Domains.Types;
using System.Linq.Expressions;
using static SharedKernel.Domains.Types.CurrentState;

namespace SharedKernel.Da.Contracts;

public interface ISharedKernelTableCmdRepo<Tb, TDbContext>
    where Tb : SharedKernelBaseTable
    where TDbContext : DbContext
{
    Task<SaveResultType> AddAsync(Tb entity, CancellationToken cancellationToken);
    Task<bool> AddAsync(IEnumerable<Tb> entities, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Tb entity, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(IEnumerable<Tb> entities, CancellationToken cancellationToken);

    Task<bool> ChangeStatusAsync(Guid id, Guid userId, CurrentStateType status, CancellationToken cancellationToken);
    Task<bool> ChangeStatusAsync(IEnumerable<Guid> ids, Guid userId, CurrentStateType status, CancellationToken cancellationToken);
    Task<bool> ChangeStatusAsync(Expression<Func<Tb, bool>> filter, Guid userId, CurrentStateType status, CancellationToken cancellationToken);

    Task<bool> ActivateAsync(Guid id, Guid userId, CancellationToken cancellationToken);
    Task<bool> ActivateAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken cancellationToken);
    Task<bool> ActivateAsync(Expression<Func<Tb, bool>> filter, Guid userId, CancellationToken cancellationToken);

    Task<bool> DeactivateAsync(Guid id, Guid userId, CancellationToken cancellationToken);
    Task<bool> DeactivateAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken cancellationToken);
    Task<bool> DeactivateAsync(Expression<Func<Tb, bool>> filter, Guid userId, CancellationToken cancellationToken);

    Task<bool> SoftDeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken cancellationToken);
    Task<bool> SoftDeleteAsync(Expression<Func<Tb, bool>> filter, Guid userId, CancellationToken cancellationToken);

    Task<bool> HardDeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> HardDeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);
    Task<bool> HardDeleteAsync(Expression<Func<Tb, bool>> filter, CancellationToken cancellationToken);
}