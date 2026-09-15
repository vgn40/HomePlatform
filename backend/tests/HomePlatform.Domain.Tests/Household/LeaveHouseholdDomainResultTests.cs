using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public sealed class LeaveHouseholdDomainResultTests
{
    [Fact]
    public void Success_has_no_error()
    {
        var result = LeaveHouseholdDomainResult.Success();
        Assert.Null(result.Error);
        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData(LeaveHouseholdError.CurrentAccountNotMember)]
    [InlineData(LeaveHouseholdError.OwnerCannotLeave)]
    public void Failure_preserves_error(LeaveHouseholdError error)
    {
        var result = LeaveHouseholdDomainResult.Failure(error);
        Assert.Equal(error, result.Error);
        Assert.False(result.IsSuccess);
    }
}
