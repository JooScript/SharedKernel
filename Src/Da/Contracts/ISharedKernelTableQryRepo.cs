using Microsoft.EntityFrameworkCore;
using SharedKernel.Da.Contracts.Models;
using System.Linq.Expressions;

namespace SharedKernel.Da.Contracts;

public interface ISharedKernelTableQryRepo<Tb, TDbContext>
    where Tb : SharedKernelBaseTable
    where TDbContext : DbContext
{
    Task<IEnumerable<Tb>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TResult?> GetByIdAsync<TResult>(
        Guid id,
        Expression<Func<Tb, TResult>> selector,
        CancellationToken cancellationToken = default,
        params Expression<Func<Tb, object>>[] includes);

    Task<Tb?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Tb>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Tb>> FindAsync(
        Expression<Func<Tb, bool>> filter,
        CancellationToken cancellationToken = default);

    Task<Tb?> GetFirstOrDefaultAsync(
        Expression<Func<Tb, bool>> filter,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<TResult>> GetListAsync<TResult>(
        Expression<Func<Tb, bool>>? filter = null,
        Expression<Func<Tb, TResult>>? selector = null,
        Expression<Func<Tb, object>>? orderBy = null,
        bool isDescending = false,
        CancellationToken cancellationToken = default,
        params Expression<Func<Tb, object>>[] includes);

    Task<PagedResult<TResult>> GetPagedListAsync<TResult>(
        int pageNumber,
        int pageSize,
        Expression<Func<Tb, bool>>? filter = null,
        Expression<Func<Tb, TResult>>? selector = null,
        Expression<Func<Tb, object>>? orderBy = null,
        bool isDescending = false,
        CancellationToken cancellationToken = default,
        params Expression<Func<Tb, object>>[] includes);

    Task<int> CountAsync(
        Expression<Func<Tb, bool>>? filter = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsExistsAsync(
        Expression<Func<Tb, bool>> filter,
        CancellationToken cancellationToken = default);

    Task<bool> IsExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Tb?> GetTrackedOrLoadAsync(
        Guid id,
        CancellationToken cancellationToken = default);

}
