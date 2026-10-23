using SharedKernel.Domains.Types;

namespace SharedKernel.Domains.Test;

public class SaveResultTypeTests
{
    [Fact]
    public void Generic_Ok_CreatesSuccessfulResult()
    {
        var id = Guid.NewGuid();

        var result = SaveResultType<string>.Ok(id);

        Assert.True(result.Success);
        Assert.Equal(id, result.Id);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Generic_Fail_CreatesUnsuccessfulResult()
    {
        var result = SaveResultType<string>.Fail("first", "second");

        Assert.False(result.Success);
        Assert.Equal(Guid.Empty, result.Id);
        Assert.Equal(new[] { "first", "second" }, result.Errors);
    }

    [Fact]
    public void Generic_From_WhenSuccess_ReturnsOk()
    {
        var id = Guid.NewGuid();

        var result = SaveResultType<string>.From(true, id, "ignored");

        Assert.True(result.Success);
        Assert.Equal(id, result.Id);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Generic_From_WhenFailure_ReturnsFail()
    {
        var result = SaveResultType<string>.From(false, Guid.NewGuid(), "error");

        Assert.False(result.Success);
        Assert.Equal(Guid.Empty, result.Id);
        Assert.Equal(new[] { "error" }, result.Errors);
    }

    [Fact]
    public void NonGeneric_Ok_CreatesSuccessfulResult()
    {
        var id = Guid.NewGuid();

        var result = SaveResultType.Ok(id);

        Assert.True(result.Success);
        Assert.Equal(id, result.Id);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void NonGeneric_Fail_CreatesUnsuccessfulResult()
    {
        var result = SaveResultType.Fail("error");

        Assert.False(result.Success);
        Assert.Equal(Guid.Empty, result.Id);
        Assert.Equal(new[] { "error" }, result.Errors);
    }

    [Fact]
    public void NonGeneric_From_WhenSuccess_ReturnsOk()
    {
        var id = Guid.NewGuid();

        var result = SaveResultType.From(true, id);

        Assert.True(result.Success);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public void NonGeneric_From_WhenFailure_ReturnsFail()
    {
        var result = SaveResultType.From(false, Guid.NewGuid(), "error");

        Assert.False(result.Success);
        Assert.Equal(new[] { "error" }, result.Errors);
    }
}
