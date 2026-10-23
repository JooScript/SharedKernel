namespace SharedKernel.Api.Tests.Models;

public class ApiResponseTests
{
    [Fact]
    public void SuccessResponse_SetsSuccessDataAndMessage()
    {
        var response = ApiResponse<int>.SuccessResponse(42, "created");

        Assert.True(response.Success);
        Assert.Equal("created", response.Message);
        Assert.Equal(42, response.Data);
        Assert.Null(response.Errors);
    }

    [Fact]
    public void SuccessResponse_UsesEmptyMessageByDefault()
    {
        var response = ApiResponse<string>.SuccessResponse("value");

        Assert.True(response.Success);
        Assert.Equal("", response.Message);
        Assert.Equal("value", response.Data);
    }

    [Fact]
    public void FailResponse_SetsFailureMessageAndErrors()
    {
        string[] errors = ["first", "second"];

        var response = ApiResponse<int>.FailResponse("failed", errors);

        Assert.False(response.Success);
        Assert.Equal("failed", response.Message);
        Assert.Equal(0, response.Data);
        Assert.Equal(errors, response.Errors);
    }

    [Fact]
    public void FailResponse_LeavesErrorsNullWhenNotProvided()
    {
        var response = ApiResponse<int>.FailResponse("failed");

        Assert.False(response.Success);
        Assert.Equal("failed", response.Message);
        Assert.Null(response.Errors);
    }

    [Fact]
    public void Defaults_AreEmptyMessageAndFalseSuccess()
    {
        var response = new ApiResponse<object>();

        Assert.False(response.Success);
        Assert.Equal("", response.Message);
        Assert.Null(response.Data);
        Assert.Null(response.Errors);
    }
}
