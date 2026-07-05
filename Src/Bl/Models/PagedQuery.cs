namespace SharedKernel.Bl.Models;

public class PagedQuery
{
    public string? Search { get; set; }

    public string? OrderBy { get; set; }

    public bool Descending { get; set; }

    public Dictionary<string, string>? Filters { get; set; }
}