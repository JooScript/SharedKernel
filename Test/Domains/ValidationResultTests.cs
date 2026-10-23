using SharedKernel.Domains.Types;

namespace SharedKernel.Domains.Test;

public class ValidationResultTests
{
    [Fact]
    public void Generic_Success_IsValidWithNoErrors()
    {
        var result = ValidationResult<int>.Success;

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Generic_Fail_IsInvalidWithErrors()
    {
        var errors = new List<int> { 1, 2 };

        var result = ValidationResult<int>.Fail(errors);

        Assert.False(result.IsValid);
        Assert.Same(errors, result.Errors);
    }

    [Fact]
    public void NonGeneric_Success_IsValidWithNoErrors()
    {
        var result = ValidationResult.Success;

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void NonGeneric_Fail_IsInvalidWithErrors()
    {
        var errors = new List<string> { "first", "second" };

        var result = ValidationResult.Fail(errors);

        Assert.False(result.IsValid);
        Assert.Same(errors, result.Errors);
    }

    [Fact]
    public void NonGeneric_IsAssignableToGenericStringResult()
    {
        ValidationResult<string> result = ValidationResult.Fail(new List<string> { "error" });

        Assert.False(result.IsValid);
        Assert.Equal(new[] { "error" }, result.Errors);
    }
}
