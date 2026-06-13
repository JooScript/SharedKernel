namespace SharedKernel.Bl.Contracts.Base;

public interface IQryBaseService<Dto> where Dto : class
{
    /// <summary>
    /// Retrieves all entities as a list of DTOs
    /// </summary>
    /// <returns>Task representing the asynchronous operation with a list of DTOs</returns>
    Task<List<Dto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single entity by its unique identifier
    /// </summary>
    /// <param name="id">The int identifier of the entity</param>
    /// <returns>Task representing the asynchronous operation with the matching DTO</returns>
    Task<Dto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
