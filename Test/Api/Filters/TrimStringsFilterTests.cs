using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using SharedKernel.Api.Filters;

namespace SharedKernel.Api.Tests.Filters;

public class TrimStringsFilterTests
{
    private readonly TrimStringsFilter _sut = new();

    [Fact]
    public void OnActionExecuting_TrimsStringProperties()
    {
        var model = new TrimModel
        {
            Name = "  John  ",
            Address = "\t Riyadh \n",
            Count = 5
        };

        _sut.OnActionExecuting(CreateContext(model));

        Assert.Equal("John", model.Name);
        Assert.Equal("Riyadh", model.Address);
        Assert.Equal(" untouched ", model.ReadOnly);
        Assert.Equal(5, model.Count);
    }

    [Fact]
    public void OnActionExecuting_LeavesNullStringNull()
    {
        var model = new TrimModel { Name = null };

        _sut.OnActionExecuting(CreateContext(model));

        Assert.Null(model.Name);
    }

    [Fact]
    public void OnActionExecuting_TrimsAllArguments()
    {
        var first = new TrimModel { Name = " first " };
        var second = new TrimModel { Name = " second " };

        _sut.OnActionExecuting(CreateContext(first, second));

        Assert.Equal("first", first.Name);
        Assert.Equal("second", second.Name);
    }

    [Fact]
    public void OnActionExecuting_IgnoresNullArguments()
    {
        var model = new TrimModel { Name = " value " };

        _sut.OnActionExecuting(CreateContext(null, model));

        Assert.Equal("value", model.Name);
    }

    [Fact]
    public void OnActionExecuted_DoesNothing()
    {
        _sut.OnActionExecuted(null!);
    }

    private static ActionExecutingContext CreateContext(params object?[] arguments)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        var actionArguments = arguments
            .Select((argument, index) => new KeyValuePair<string, object?>($"arg{index}", argument))
            .ToDictionary();

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            actionArguments,
            new object());
    }

    private sealed class TrimModel
    {
        public string? Name { get; set; }

        public string? Address { get; set; }

        public string ReadOnly => " untouched ";

        public int Count { get; set; }
    }
}
