using HomePlatform.Domain.Household;
using HouseholdAggregate = HomePlatform.Domain.Household.Household;

namespace HomePlatform.Domain.Tests.Household;

public sealed class RemoveHouseholdMemberTests
{
    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public void Owner_removes_only_target_without_changing_ownership(HouseholdRole role)
    {
        var owner = Guid.NewGuid();
        var household = new HouseholdAggregate("Home", owner);
        Assert.True(household.AddMember(owner, role, Guid.NewGuid()).IsSuccess);
        Assert.True(household.AddMember(owner, HouseholdRole.Owner, Guid.NewGuid()).IsSuccess);
        var target = household.Members.Single(m => m.Role == role);
        var survivors = household.Members.Where(m => m != target).ToArray();
        var version = household.OwnershipVersion;
        var earlier = DateTime.UtcNow.AddDays(-1);
        typeof(HouseholdAggregate).GetProperty(nameof(household.UpdatedAt))!.SetValue(household, earlier);

        var result = household.RemoveMember(owner, target.MembershipId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal(survivors, household.Members);
        Assert.Equal(version, household.OwnershipVersion);
        Assert.True(household.UpdatedAt > earlier);
        Assert.Equal(2, household.Members.Count(m => m.Role == HouseholdRole.Owner));
    }

    [Theory]
    [InlineData("member", RemoveHouseholdMemberError.CurrentPersonNotOwner)]
    [InlineData("guest", RemoveHouseholdMemberError.CurrentPersonNotOwner)]
    [InlineData("outsider", RemoveHouseholdMemberError.CurrentPersonNotMember)]
    [InlineData("owner-target", RemoveHouseholdMemberError.CannotRemoveOwner)]
    [InlineData("self", RemoveHouseholdMemberError.CannotRemoveSelf)]
    [InlineData("missing", RemoveHouseholdMemberError.MembershipNotFound)]
    [InlineData("other-household", RemoveHouseholdMemberError.MembershipNotFound)]
    public void Rejection_preserves_all_state(string scenario, RemoveHouseholdMemberError error)
    {
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        var guest = Guid.NewGuid();
        var household = new HouseholdAggregate("Home", owner);
        Assert.True(household.AddMember(owner, HouseholdRole.Member, member).IsSuccess);
        Assert.True(household.AddMember(owner, HouseholdRole.Guest, guest).IsSuccess);
        Assert.True(household.AddMember(owner, HouseholdRole.Owner, Guid.NewGuid()).IsSuccess);
        var before = household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray();
        var timestamp = household.UpdatedAt;
        var version = household.OwnershipVersion;
        var actor = scenario switch
        {
            "member" => member,
            "guest" => guest,
            "outsider" => Guid.NewGuid(),
            _ => owner
        };
        var target = scenario switch
        {
            "self" => household.Members.Single(m => m.PersonId == owner).MembershipId,
            "owner-target" => household.Members.Single(m => m.Role == HouseholdRole.Owner && m.PersonId != owner).MembershipId,
            "missing" => Guid.NewGuid(),
            "other-household" => new HouseholdAggregate("Other", owner).Members.Single().MembershipId,
            _ => household.Members.Single(m => m.PersonId == member).MembershipId
        };

        var result = household.RemoveMember(actor, target);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
        Assert.Equal(before, household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray());
        Assert.Equal(timestamp, household.UpdatedAt);
        Assert.Equal(version, household.OwnershipVersion);
    }

    [Theory]
    [InlineData(HouseholdRole.Member, RemoveHouseholdMemberError.CurrentPersonNotOwner)]
    [InlineData(HouseholdRole.Guest, RemoveHouseholdMemberError.CurrentPersonNotOwner)]
    public void Authorization_precedes_target_lookup(HouseholdRole role, RemoveHouseholdMemberError error)
    {
        var owner = Guid.NewGuid();
        var actor = Guid.NewGuid();
        var household = new HouseholdAggregate("Home", owner);
        Assert.True(household.AddMember(owner, role, actor).IsSuccess);
        Assert.Equal(error, household.RemoveMember(actor, Guid.NewGuid()).Error);
        Assert.Equal(RemoveHouseholdMemberError.CurrentPersonNotMember,
            household.RemoveMember(Guid.NewGuid(), Guid.NewGuid()).Error);
    }
}
