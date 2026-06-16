using SharedKernel.Bl.Enums;

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
    Task<(bool success, Guid id)> AddAsync(Dto entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity based on the provided DTO
    /// </summary>
    /// <param name="entity">The DTO containing updated entity data</param>
    /// <returns>
    /// Task representing the asynchronous operation with a boolean indicating success
    /// </returns>
    Task<bool> UpdateAsync(Dto entity, bool fireEvent = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the status of an entity
    /// </summary>
    /// <param name="id">The int identifier of the entity</param>
    /// <param name="status">The new status value (default: 1)</param>
    /// <returns>
    /// Task representing the asynchronous operation with a boolean indicating success
    /// </returns>
    Task<bool> ActivateAsync(Guid id, bool fireEvent = true, CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(Guid id, bool fireEvent = true, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, ServicesEnums.enDeleteType deleteType = ServicesEnums.enDeleteType.HardDelete, bool fireEvent = true, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(List<Guid> Ids, ServicesEnums.enDeleteType deleteType = ServicesEnums.enDeleteType.HardDelete, bool fireEvent = true, CancellationToken cancellationToken = default);

}