using SharedKernel.Bl.Models;

namespace SharedKernel.Bl.Test.Models;

public class ModelTests
{
    [Fact]
    public void PagedQuery_HasExpectedDefaults()
    {
        var query = new PagedQuery();

        Assert.Equal(1, query.PageNumber);
        Assert.Equal(10, query.PageSize);
        Assert.Null(query.Search);
        Assert.Null(query.OrderBy);
        Assert.False(query.Descending);
        Assert.Null(query.Filters);
    }

    [Fact]
    public void PagedQuery_ToString_IncludesValuesAndFilters()
    {
        var query = new PagedQuery
        {
            PageNumber = 2,
            PageSize = 25,
            Search = "text",
            OrderBy = "name",
            Descending = true,
            Filters = new List<Filter>
            {
                new() { Key = "status", Mode = FilterMode.Equals, Value = "active" }
            }
        };

        var result = query.ToString();

        Assert.Contains("PageNumber: 2", result);
        Assert.Contains("PageSize: 25", result);
        Assert.Contains("Search: text", result);
        Assert.Contains("OrderBy: name", result);
        Assert.Contains("Descending: True", result);
        Assert.Contains("Key: status", result);
    }

    [Fact]
    public void PagedQuery_ToString_WithoutFilters_ShowsNone()
    {
        var result = new PagedQuery().ToString();

        Assert.Contains("Filters: [None]", result);
    }

    [Fact]
    public void Filter_ToString_IncludesValues()
    {
        var filter = new Filter
        {
            Key = "vat",
            Mode = FilterMode.Include,
            Value = "300"
        };

        var result = filter.ToString();

        Assert.Contains("Key: vat", result);
        Assert.Contains("Mode: Include", result);
        Assert.Contains("Value: 300", result);
    }

    [Fact]
    public void EntityValidationResult_StoresStateAndMessage()
    {
        var result = new EntityValidationResult(false, "not allowed");

        Assert.False(result.Success);
        Assert.Equal("not allowed", result.Message);
    }
}
