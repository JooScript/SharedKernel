using SharedKernel.Da.Contracts.Models;
using System.Linq.Expressions;

namespace SharedKernel.Da.Contracts;

public interface ISharedKernelQueryRepo<T> where T : class
{
    Task<int> CountAsync(Expression<Func<T, bool>>? filter, CancellationToken cancellationToken);

    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken);

    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken);

    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken);

    Task<IReadOnlyList<T>> GetListAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken);

    Task<bool> IsExistsAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken);

    Task<TResult?> GetByIdAsync<TResult>(
        Guid id,
        Expression<Func<T, TResult>> selector,
        CancellationToken cancellationToken,
        params Expression<Func<T, object>>[] includes);

    Task<IEnumerable<T>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);

    Task<IEnumerable<TResult>> GetListAsync<TResult>(
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, TResult>>? selector = null,
        Expression<Func<T, object>>? orderBy = null,
        bool isDescending = false,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[] includes);

    Task<PagedResult<TResult>> GetPagedListAsync<TResult>(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Expression<Func<T, TResult>>? selector = null,
        Expression<Func<T, object>>? orderBy = null,
        bool isDescending = false,
        CancellationToken cancellationToken = default,
        params Expression<Func<T, object>>[] includes);

    Task<bool> IsExistsAsync(Guid id, CancellationToken cancellationToken);

    Task<T?> GetTrackedOrLoadAsync(Guid id, CancellationToken cancellationToken);

    Task<IEnumerable<TResult>> FindAsync<TResult>(Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>>? selector,
        Expression<Func<T, object>>? orderBy,
        bool isDescending,
        CancellationToken cancellationToken,
        params Expression<Func<T, object>>[] includes);

}