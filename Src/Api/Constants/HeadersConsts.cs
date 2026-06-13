namespace SharedKernel.Api.Constants;

/// <summary>
/// Contains constant values for HTTP header keys used across the API.
/// </summary>
public static class HeadersConsts
{
    /// <summary>
    /// Represents the HTTP header key used to return pagination metadata.
    /// Commonly used to transmit paging information such as total count,
    /// page number, and page size in API responses.
    /// </summary>
    public const string Pagination = "X-Pagination";
}