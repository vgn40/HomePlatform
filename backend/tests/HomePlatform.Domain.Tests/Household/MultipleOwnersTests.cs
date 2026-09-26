using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public sealed class MultipleOwnersTests
{
    [Fact]
    public void AddMember_allows_three_owners_without_changing_existing_memberships()
    {
        var firstId = Guid.NewGuid();
        var household = new Domain.Household.Household("Home", firstId);
        var first = Assert.Single(household.Members);
        var secondId = Guid.NewGuid();
        Assert.True(household.AddMember(firstId, HouseholdRole.Owner, secondId).IsSuccess);
        Assert.True(household.AddMember(secondId, HouseholdRole.Owner, Guid.NewGuid()).IsSuccess);
        Assert.Equal(3, household.Members.Count);
        Assert.All(household.Members, member => Assert.Equal(HouseholdRole.Owner, member.Role));
        Assert.Same(first, household.Members.Single(member => member.PersonId == firstId));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void Leave_removes_one_owner_and_preserves_all_other_memberships(int ownerCount)
    {
        var actorId = Guid.NewGuid();
        var household = new Domain.Household.Household("Home", actorId);
        for (var i = 1; i < ownerCount; i++)
            Assert.True(household.AddMember(actorId, HouseholdRole.Owner, Guid.NewGuid()).IsSuccess);
        Assert.True(household.AddMember(actorId, HouseholdRole.Member, Guid.NewGuid()).IsSuccess);
        var expected = household.Members.Where(m => m.PersonId != actorId)
            .Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray();
        var earlier = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        typeof(Domain.Household.Household).GetProperty(nameof(household.UpdatedAt))!.SetValue(household, earlier);

        var result = household.Leave(actorId);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray());
        Assert.Equal(ownerCount - 1, household.Members.Count(m => m.Role == HouseholdRole.Owner));
        Assert.True(household.UpdatedAt > earlier);
    }

    [Fact]
    public void Sole_owner_cannot_leave_and_must_explicitly_close()
    {
        var ownerId = Guid.NewGuid();
        var household = new Domain.Household.Household("Home", ownerId);
        var member = Assert.Single(household.Members);
        var updatedAt = household.UpdatedAt;

        var result = household.Leave(ownerId);

        Assert.False(result.IsSuccess);
        Assert.Equal(LeaveHouseholdError.LastOwnerCannotLeave, result.Error);
        Assert.Same(member, Assert.Single(household.Members));
        Assert.Equal(HouseholdRole.Owner, member.Role);
        Assert.Equal(updatedAt, household.UpdatedAt);
        Assert.True(household.Close(ownerId).IsSuccess);
    }

    [Fact]
    public void Transfer_changes_only_actor_and_target_and_preserves_other_owners()
    {
        var actorId = Guid.NewGuid();
        var household = new Domain.Household.Household("Home", actorId);
        var otherId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        Assert.True(household.AddMember(actorId, HouseholdRole.Owner, otherId).IsSuccess);
        Assert.True(household.AddMember(actorId, HouseholdRole.Member, targetId).IsSuccess);
        var target = household.Members.Single(m => m.PersonId == targetId);
        var identities = household.Members.Select(m => (m.MembershipId, m.PersonId)).ToArray();

        Assert.True(household.TransferOwnership(actorId, target.MembershipId).IsSuccess);

        Assert.Equal(identities, household.Members.Select(m => (m.MembershipId, m.PersonId)).ToArray());
        Assert.Equal(HouseholdRole.Member, household.Members.Single(m => m.PersonId == actorId).Role);
        Assert.Equal(HouseholdRole.Owner, household.Members.Single(m => m.PersonId == otherId).Role);
        Assert.Equal(HouseholdRole.Owner, target.Role);
    }

    [Fact]
    public void Transfer_to_existing_owner_is_invalid_and_does_not_mutate_state()
    {
        var actorId = Guid.NewGuid();
        var household = new Domain.Household.Household("Home", actorId);
        var targetId = Guid.NewGuid();
        Assert.True(household.AddMember(actorId, HouseholdRole.Owner, targetId).IsSuccess);
        var target = household.Members.Single(m => m.PersonId == targetId);
        var before = household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray();
        var updatedAt = household.UpdatedAt;

        Assert.Equal(TransferOwnershipError.NewOwnerAlreadyOwner,
            household.ValidateTransferOwnership(actorId, target.MembershipId).Error);
        Assert.Equal(TransferOwnershipError.NewOwnerAlreadyOwner,
            household.TransferOwnership(actorId, target.MembershipId).Error);

        Assert.Equal(before, household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray());
        Assert.Equal(updatedAt, household.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Any_of_three_owners_can_close(int actingOwner)
    {
        var owners = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var household = new Domain.Household.Household("Home", owners[0]);
        foreach (var owner in owners.Skip(1))
            Assert.True(household.AddMember(owners[0], HouseholdRole.Owner, owner).IsSuccess);

        Assert.True(household.Close(owners[actingOwner]).IsSuccess);
    }
}
