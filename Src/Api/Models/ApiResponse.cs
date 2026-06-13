namespace SharedKernel.Api.Models;

/// <summary>
/// Represents a standardized API response wrapper.
/// </summary>
/// <typeparam name="T">
/// The type of the response data.
/// </typeparam>
/// <remarks>
/// This model provides a consistent structure for API responses,
/// including operation status, messages, returned data,
/// and validation or processing errors.
/// </remarks>
public class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the response message describing the operation result.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Gets or sets the response payload data.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets the collection of errors associated with the response.
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Creates a successful API response.
    /// </summary>
    /// <param name="data">
    /// The response payload data.
    /// </param>
    /// <param name="message">
    /// An optional success message.
    /// </param>
    /// <returns>
    /// A successful <see cref="ApiResponse{T}"/> instance.
    /// </returns>
    public static ApiResponse<T> SuccessResponse(T data, string message = "")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// Creates a failed API response.
    /// </summary>
    /// <param name="message">
    /// The error message describing the failure reason.
    /// </param>
    /// <param name="errors">
    /// Optional detailed validation or processing errors.
    /// </param>
    /// <returns>
    /// A failed <see cref="ApiResponse{T}"/> instance.
    /// </returns>
    public static ApiResponse<T> FailResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}