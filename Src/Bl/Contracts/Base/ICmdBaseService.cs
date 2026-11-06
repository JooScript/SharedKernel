using SharedKernel.Bl.Enums;
using SharedKernel.Domains.Types;
using static SharedKernel.Bl.Enums.ServicesEnums;

namespace SharedKernel.Bl.Contracts.Base;

public interface ICmdBaseService<Dto> where Dto : class
{
    /// <summary>
    /// Adds a new entity based on the provided DTO
    /// </summary>
    /// <param name="entity">The DTO containing data for the new entity</param>
    /// <returns>
    /// Task representing the asynchronous operation with a tuple containing:
    /// success - indicates if the operation succeeded
    /// id - the int of the newly created entity (valid when success is true)
    /// </returns>
    Task<SaveResultType> AddAsync(Dto entity, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing entity based on the provided DTO
    /// </summary>
    /// <param name="entity">The DTO containing updated entity data</param>
    /// <returns>
    /// Task representing the asynchronous operation with a boolean indicating success
    /// </returns>
    Task<bool> UpdateAsync(Dto entity, CancellationToken cancellationToken);

    Task<bool> DuplicateAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Changes the status of an entity
    /// </summary>
    /// <param name="id">The Guid identifier of the entity</param>
    /// <param name="status">The new status value (default: 1)</param>
    /// <returns>
    /// Task representing the asynchronous operation with a boolean indicating success
    /// </returns>
    Task<bool> ActivateAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> DeactivateAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, DeleteType deleteType, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(List<Guid> Ids, DeleteType deleteType, CancellationToken cancellationToken);

}