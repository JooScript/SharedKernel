namespace SharedKernel.Api.Tests.Models;

public class HeaderPaginationTests
{
    [Fact]
    public void Properties_CanBeAssignedAndRead()
    {
        var pagination = new HeaderPagination
        {
            CurrentPage = 2,
            TotalPages = 5,
            PageSize = 20,
            TotalCount = 100,
            HasPrevious = true,
            HasNext = true
        };

        Assert.Equal(2, pagination.CurrentPage);
        Assert.Equal(5, pagination.TotalPages);
        Assert.Equal(20, pagination.PageSize);
        Assert.Equal(100, pagination.TotalCount);
        Assert.True(pagination.HasPrevious);
        Assert.True(pagination.HasNext);
    }

    [Fact]
    public void NewInstance_HasZeroValues()
    {
        var pagination = new HeaderPagination();

        Assert.Equal(0, pagination.CurrentPage);
        Assert.Equal(0, pagination.TotalPages);
        Assert.Equal(0, pagination.PageSize);
        Assert.Equal(0, pagination.TotalCount);
        Assert.False(pagination.HasPrevious);
        Assert.False(pagination.HasNext);
    }
}
