using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public sealed class LeaveHouseholdTests
{
    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public void Leave_removes_only_the_acting_membership_and_advances_updated_at(HouseholdRole role)
    {
        var household = new Domain.Household.Household("Home", Guid.NewGuid());
        var personId = Guid.NewGuid();
        Assert.True(household.AddMember(role, personId).IsSuccess);
        Assert.True(household.AddMember(HouseholdRole.Member, Guid.NewGuid()).IsSuccess);
        Assert.True(household.AddMember(HouseholdRole.Guest, Guid.NewGuid()).IsSuccess);
        var target = Assert.Single(household.Members, member => member.PersonId == personId);
        var before = Snapshot(household);
        var earlier = SetEarlierUpdatedAt(household);
        var start = DateTime.UtcNow;

        var result = household.Leave(personId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(before.Length - 1, household.Members.Count);
        Assert.DoesNotContain(household.Members, member => member.MembershipId == target.MembershipId);
        Assert.Equal(before.Where(member => member.MembershipId != target.MembershipId).ToArray(), Snapshot(household));
        Assert.True(household.UpdatedAt > earlier);
        Assert.InRange(household.UpdatedAt, start, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(true, LeaveHouseholdError.OwnerCannotLeave)]
    [InlineData(false, LeaveHouseholdError.CurrentPersonNotMember)]
    public void Leave_rejection_preserves_every_membership_and_updated_at(bool owner, LeaveHouseholdError error)
    {
        var ownerId = Guid.NewGuid();
        var household = new Domain.Household.Household("Home", ownerId);
        Assert.True(household.AddMember(HouseholdRole.Member, Guid.NewGuid()).IsSuccess);
        Assert.True(household.AddMember(HouseholdRole.Guest, Guid.NewGuid()).IsSuccess);
        var before = Snapshot(household);
        var earlier = SetEarlierUpdatedAt(household);

        var result = household.Leave(owner ? ownerId : Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
        Assert.Equal(before, Snapshot(household));
        Assert.Equal(earlier, household.UpdatedAt);
    }

    private static DateTime SetEarlierUpdatedAt(Domain.Household.Household household)
    {
        var earlier = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        // Match existing domain tests without sleeping or changing the production clock API.
        var property = typeof(Domain.Household.Household).GetProperty(nameof(household.UpdatedAt));
        Assert.NotNull(property);
        property.SetValue(household, earlier);
        Assert.Equal(earlier, household.UpdatedAt);
        return earlier;
    }

    private static (Guid MembershipId, Guid PersonId, HouseholdRole Role)[] Snapshot(Domain.Household.Household household)
        => household.Members.OrderBy(member => member.MembershipId)
            .Select(member => (member.MembershipId, member.PersonId, member.Role)).ToArray();
}
