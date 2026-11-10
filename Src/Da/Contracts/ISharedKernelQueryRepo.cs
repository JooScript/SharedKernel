using SharedKernel.Da.Contracts.Models;
using System.Linq.Expressions;

namespace SharedKernel.Da.Contracts;

public interface ISharedKernelQueryRepo<T> where T : class
{
    Task<int> CountAsync(Expression<Func<T, bool>>? filter, CancellationToken cancellationToken);

    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken);

    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken);

    Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken);

    Task<IEnumerable<T>> GetListAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken);

    Task<bool> IsExistsAsync(Expression<Func<T, bool>> filter, CancellationToken cancellationToken);

    Task<IEnumerable<TResult>> GetListAsync<TResult>(
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>>? selector,
        Expression<Func<T, object>>? orderBy,
        bool isDescending,
        CancellationToken cancellationToken,
        params Expression<Func<T, object>>[] includes);

    Task<PagedResult<TResult>> GetPagedListAsync<TResult>(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>>? selector,
        Expression<Func<T, object>>? orderBy,
        bool isDescending,
        CancellationToken cancellationToken,
        params Expression<Func<T, object>>[] includes);

    Task<IEnumerable<TResult>> FindAsync<TResult>(
        Expression<Func<T, bool>>? filter,
        Expression<Func<T, TResult>>? selector,
        Expression<Func<T, object>>? orderBy,
        bool isDescending,
        CancellationToken cancellationToken,
        params Expression<Func<T, object>>[] includes);

}