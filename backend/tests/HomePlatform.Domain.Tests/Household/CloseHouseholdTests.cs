using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public sealed class CloseHouseholdTests
{
    [Theory]
    [InlineData("owner", true)]
    [InlineData("member", false)]
    [InlineData("guest", false)]
    [InlineData("nonmember", false)]
    [InlineData("loginless", false)]
    [InlineData("empty-account", false)]
    public void Close_authorizes_only_owner_and_preserves_entire_aggregate(string caller, bool allowed)
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var guestId = Guid.NewGuid();
        var household = new Domain.Household.Household("Home", ownerId);
        Assert.True(household.AddMember(HouseholdRole.Member, memberId).IsSuccess);
        Assert.True(household.AddMember(HouseholdRole.Guest, guestId).IsSuccess);
        Assert.True(household.AddMember(HouseholdRole.Member).IsSuccess);
        Assert.True(household.AddMember(HouseholdRole.Guest).IsSuccess);
        var earlier = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        // Match existing tests: make timestamp mutation observable without sleeping.
        typeof(Domain.Household.Household).GetProperty(nameof(household.UpdatedAt))!.SetValue(household, earlier);
        var metadata = (household.Id, household.Name, household.CreatedAt, household.UpdatedAt);
        var members = household.Members.ToArray();
        var values = members.Select(m => (m.MembershipId, m.AccountId, m.Role)).ToArray();
        var accountId = caller switch
        {
            "owner" => ownerId,
            "member" => memberId,
            "guest" => guestId,
            "loginless" => household.Members.First(m => m.AccountId is null).MembershipId,
            "empty-account" => Guid.Empty,
            _ => Guid.NewGuid()
        };

        var result = household.Close(accountId);

        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed ? (CloseHouseholdError?)null : CloseHouseholdError.CurrentAccountNotOwner, result.Error);
        Assert.Equal(metadata, (household.Id, household.Name, household.CreatedAt, household.UpdatedAt));
        Assert.Equal(members.Length, household.Members.Count);
        for (var i = 0; i < members.Length; i++)
            Assert.Same(members[i], household.Members.ElementAt(i));
        Assert.Equal(values, household.Members.Select(m => (m.MembershipId, m.AccountId, m.Role)).ToArray());
    }

    [Fact]
    public void Success_has_no_error()
    {
        var result = CloseHouseholdDomainResult.Success();
        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_preserves_exact_error()
    {
        var result = CloseHouseholdDomainResult.Failure(CloseHouseholdError.CurrentAccountNotOwner);
        Assert.False(result.IsSuccess);
        Assert.Equal(CloseHouseholdError.CurrentAccountNotOwner, result.Error);
    }
}
