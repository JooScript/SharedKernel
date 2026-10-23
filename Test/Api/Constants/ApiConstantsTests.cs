using SharedKernel.Api.Constants;

namespace SharedKernel.Api.Tests.Constants;

public class ApiConstantsTests
{
    [Fact]
    public void PaginationHeader_HasExpectedValue()
    {
        Assert.Equal("X-Pagination", HeadersConsts.Pagination);
    }

    [Theory]
    [InlineData("fixed", RateLimitingPolicies.FixedWindow)]
    [InlineData("sliding", RateLimitingPolicies.SlidingWindow)]
    [InlineData("token", RateLimitingPolicies.TokenBucket)]
    [InlineData("concurrency", RateLimitingPolicies.Concurrency)]
    public void RateLimitingPolicy_HasExpectedValue(string expected, string actual)
    {
        Assert.Equal(expected, actual);
    }
}
