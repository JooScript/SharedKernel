using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Bl.Models;

public class PagedQuery
{
    [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0.")]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 10;

    [MaxLength(200, ErrorMessage = "Search cannot exceed 200 characters.")]
    public string? Search { get; set; }

    [MaxLength(100, ErrorMessage = "OrderBy cannot exceed 100 characters.")]
    public string? OrderBy { get; set; }

    public bool Descending { get; set; } = false;

    public List<Filter>? Filters { get; set; }

    public override string ToString()
    {
        var filters = Filters is null || Filters.Count == 0
            ? "None"
            : string.Join(", ", Filters);

        return $@"
PageNumber: {PageNumber}, 
PageSize: {PageSize}, 
Search: {Search ?? "null"}, 
OrderBy: {OrderBy ?? "null"}, 
Descending: {Descending}, 
Filters: [{filters}]";
    }
}

public class Filter
{
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public FilterMode Mode { get; set; }

    [Required]
    public string Value { get; set; } = string.Empty;

    public override string ToString()
    {
        return $@"
Key: {Key}, 
Mode: {Mode}, 
Value: {Value}
";
    }

}

public enum FilterMode
{
    Equals = 0,
    NotEquals = 1,
    Include = 2,
    Exclude = 3,
    GreaterThan = 4,
    GreaterThanOrEqual = 5,
    LessThan = 6,
    LessThanOrEqual = 7
}