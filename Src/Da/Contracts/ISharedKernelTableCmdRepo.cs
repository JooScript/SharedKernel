using Microsoft.EntityFrameworkCore;
using static SharedKernel.Domains.Enums.CurrentState;

namespace SharedKernel.Da.Contracts;

public interface ISharedKernelTableCmdRepo<Tb, TDbContext>
    where Tb : SharedKernelBaseTable
    where TDbContext : DbContext
{
    Task<(bool success, Guid newId)> AddAsync(Tb entity, CancellationToken cancellationToken = default);

    Task<(bool success, IEnumerable<Guid> newIds)> AddAsync(IEnumerable<Tb> entities, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Tb entity, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(IEnumerable<Tb> entities, CancellationToken cancellationToken = default);

    Task<bool> ChangeStatusAsync(Guid id, Guid userId, CurrentStateType status = CurrentStateType.Active, CancellationToken cancellationToken = default);

    Task<bool> ChangeStatusAsync(IEnumerable<Guid> ids, Guid userId, CurrentStateType status = CurrentStateType.Active, CancellationToken cancellationToken = default);

    Task<bool> HardDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> HardDeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken cancellationToken = default);

}