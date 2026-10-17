using System.Linq.Expressions;

namespace SharedKernel.Da.Contracts;

public interface ISharedKernelViewRepo<Vw> where Vw : class
{
    Task<int> CountAsync(Expression<Func<Vw, bool>>? filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<Vw>> FindAsync(Expression<Func<Vw, bool>> filter, CancellationToken cancellationToken);
    Task<IReadOnlyList<Vw>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Vw?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Vw?> GetFirstOrDefaultAsync(Expression<Func<Vw, bool>> filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Vw>> GetListAsync(Expression<Func<Vw, bool>> filter, CancellationToken cancellationToken = default);
    Task<bool> IsExistsAsync(Expression<Func<Vw, bool>> filter, CancellationToken cancellationToken);
}
