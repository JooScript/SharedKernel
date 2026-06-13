namespace SharedKernel.Api.Models;

/// <summary>
/// Represents pagination metadata returned in API response headers.
/// </summary>
/// <remarks>
/// This model contains paging information such as the current page,
/// total pages, total records count, and navigation indicators.
/// </remarks>
public class HeaderPagination
{
    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int CurrentPage { get; set; }

    /// <summary>
    /// Gets or sets the total number of available pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets the number of records per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of records.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a previous page exists.
    /// </summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a next page exists.
    /// </summary>
    public bool HasNext { get; set; }
}