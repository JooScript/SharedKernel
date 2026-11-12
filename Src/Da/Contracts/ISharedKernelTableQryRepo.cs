using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace SharedKernel.Da.Contracts;

public interface ISharedKernelTableQryRepo<T, TDbContext> : ISharedKernelQueryRepo<T>
    where T : SharedKernelBaseTable
    where TDbContext : DbContext
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<TResult?> GetByIdAsync<TResult>(
        Guid id,
        Expression<Func<T, TResult>>? selector,
        CancellationToken cancellationToken,
        params Expression<Func<T, object>>[] includes);

    Task<IEnumerable<T>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);

    Task<bool> IsExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<T?> GetTrackedOrLoadAsync(Guid id, CancellationToken cancellationToken);
}
