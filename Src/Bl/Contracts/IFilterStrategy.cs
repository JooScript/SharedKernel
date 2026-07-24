using LinqKit;
using SharedKernel.Bl.Models;

namespace SharedKernel.Bl.Contracts;

public interface IFilterStrategy<TEntity>
{
    string Key { get; }

    ExpressionStarter<TEntity> Apply(
        ExpressionStarter<TEntity> filter,
        Filter requestFilter);
}