using SharedKernel.Bl.Models;
using System.Linq.Expressions;

namespace SharedKernel.Bl.Contracts.Base;

public interface IValidationBaseService<Tb, Dto> where Tb : SharedKernelBaseTable where Dto : class
{
    Task<bool> IsExistsAsync(Expression<Func<Tb, bool>> filter, CancellationToken cancellationToken = default);

    Task<bool> IsExistsAsync(Guid Id, CancellationToken cancellationToken = default);

    Task<EntityValidationResult> CanDeleteAsync(Guid id);

    Task<EntityValidationResult> CanUpdateAsync(Dto entity);
}
