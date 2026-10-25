using HomePlatform.Domain.Household;
using HouseholdAggregate = HomePlatform.Domain.Household.Household;

namespace HomePlatform.Domain.Tests.Household;

public sealed class OwnershipVersionTests
{
    [Fact]
    public void Constructor_initializes_nonempty_ownership_version()
    {
        Assert.NotEqual(Guid.Empty, new HouseholdAggregate("Home", Guid.NewGuid()).OwnershipVersion);
    }

    [Theory]
    [InlineData(HouseholdRole.Owner, true)]
    [InlineData(HouseholdRole.Member, false)]
    [InlineData(HouseholdRole.Guest, false)]
    public void Adding_role_rotates_version_only_for_owner(HouseholdRole role, bool rotates)
    {
        var owner = Guid.NewGuid();
        var household = new HouseholdAggregate("Home", owner);
        var before = household.OwnershipVersion;
        Assert.True(household.AddMember(owner, role, Guid.NewGuid()).IsSuccess);
        Assert.Equal(rotates, before != household.OwnershipVersion);
    }

    [Theory]
    [InlineData(HouseholdRole.Owner, true)]
    [InlineData(HouseholdRole.Member, false)]
    [InlineData(HouseholdRole.Guest, false)]
    public void Leaving_rotates_version_only_for_owner(HouseholdRole role, bool rotates)
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var household = new HouseholdAggregate("Home", owner);
        Assert.True(household.AddMember(owner, role, other).IsSuccess);
        var before = household.OwnershipVersion;
        Assert.True(household.Leave(other).IsSuccess);
        Assert.Equal(rotates, before != household.OwnershipVersion);
    }

    [Fact]
    public void Transfer_rotates_version_and_rejected_operations_preserve_it()
    {
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var household = new HouseholdAggregate("Home", owner);
        Assert.True(household.AddMember(owner, HouseholdRole.Member, other).IsSuccess);
        var before = household.OwnershipVersion;
        Assert.False(household.Leave(owner).IsSuccess);
        Assert.False(household.AddMember(other, HouseholdRole.Guest, Guid.NewGuid()).IsSuccess);
        Assert.False(household.TransferOwnership(other, household.Members.First().MembershipId).IsSuccess);
        Assert.Equal(before, household.OwnershipVersion);
        Assert.True(household.TransferOwnership(owner,
            household.Members.Single(m => m.PersonId == other).MembershipId).IsSuccess);
        Assert.NotEqual(before, household.OwnershipVersion);
    }
}
