using System.ComponentModel.DataAnnotations;
using SharedKernel.Bl.Dtos.Validation;

namespace SharedKernel.Bl.Test.Validation;

public class ValidationAttributeTests
{
    private sealed class FutureDateHolder
    {
        [FutureDate(ErrorMessage = "must be future")]
        public object? Value { get; set; }
    }

    private sealed class NotFutureDateHolder
    {
        [NotFutureDate(ErrorMessage = "must not be future")]
        public object? Value { get; set; }
    }

    private static (bool IsValid, IReadOnlyList<string> Errors) Validate(object model)
    {
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(
            model,
            new ValidationContext(model),
            results,
            validateAllProperties: true);

        return (valid, results.Select(r => r.ErrorMessage ?? string.Empty).ToList());
    }

    [Fact]
    public void FutureDate_WhenNull_IsValid()
    {
        var result = Validate(new FutureDateHolder { Value = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void FutureDate_WhenFuture_IsValid()
    {
        var result = Validate(new FutureDateHolder { Value = DateTime.UtcNow.AddDays(1) });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void FutureDate_WhenPast_IsInvalid()
    {
        var result = Validate(new FutureDateHolder { Value = DateTime.UtcNow.AddDays(-1) });

        Assert.False(result.IsValid);
        Assert.Contains("must be future", result.Errors);
    }

    [Fact]
    public void FutureDate_WhenNotDateTime_IsInvalid()
    {
        var result = Validate(new FutureDateHolder { Value = "tomorrow" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void NotFutureDate_WhenNull_IsValid()
    {
        var result = Validate(new NotFutureDateHolder { Value = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void NotFutureDate_WhenToday_IsValid()
    {
        var result = Validate(new NotFutureDateHolder { Value = DateOnly.FromDateTime(DateTime.UtcNow) });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void NotFutureDate_WhenPastDateTime_IsValid()
    {
        var result = Validate(new NotFutureDateHolder { Value = DateTime.UtcNow.AddDays(-2) });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void NotFutureDate_WhenTomorrow_IsInvalid()
    {
        var result = Validate(new NotFutureDateHolder { Value = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)) });

        Assert.False(result.IsValid);
        Assert.Contains("must not be future", result.Errors);
    }

    [Fact]
    public void NotFutureDate_WhenNotDate_IsInvalid()
    {
        var result = Validate(new NotFutureDateHolder { Value = "later" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void GreaterThanZero_WhenNull_IsValid()
    {
        Assert.True(new GreaterThanZeroAttribute().IsValid(null));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(1.5)]
    [InlineData(1L)]
    public void GreaterThanZero_WhenPositive_IsValid(object value)
    {
        Assert.True(new GreaterThanZeroAttribute().IsValid(value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1.5)]
    public void GreaterThanZero_WhenNotPositive_IsInvalid(object value)
    {
        Assert.False(new GreaterThanZeroAttribute().IsValid(value));
    }

    [Fact]
    public void GreaterThanZero_WhenUnsupportedType_IsInvalid()
    {
        Assert.False(new GreaterThanZeroAttribute().IsValid("1"));
    }
}
