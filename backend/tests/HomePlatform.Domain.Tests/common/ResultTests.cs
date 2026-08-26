using HomePlatform.Domain.Common;

namespace HomePlatform.Domain.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_returns_successful_result()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Failure_returns_failed_result_with_error()
    {
        var result = Result.Failure("Something went wrong.");

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "Something went wrong.",
            result.ErrorMessage);
    }
}