using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public class HouseholdMembersTest
{
    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    [InlineData(HouseholdRole.Owner)]
    public void AddMember_with_person_succeeds_and_creates_expected_membership(
        HouseholdRole role)
    {
        var household = CreateHousehold();
        var personId = Guid.NewGuid();

        var result = household.AddMember(household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId, role, personId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);

        var member = Assert.Single(
            household.Members,
            membership => membership.PersonId == personId);
        Assert.NotEqual(Guid.Empty, member.MembershipId);
        Assert.Equal(personId, member.PersonId);
        Assert.Equal(role, member.Role);
        Assert.Equal(2, household.Members.Count);
        Assert.Equal(role == HouseholdRole.Owner ? 2 : 1,
            household.Members.Count(membership => membership.Role == HouseholdRole.Owner));
    }

    [Fact]
    public void AddMember_with_another_person_succeeds()
    {
        var household = CreateHousehold();

        var result = household.AddMember(household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId, HouseholdRole.Member, Guid.NewGuid());

        Assert.True(result.IsSuccess);

        var member = Assert.Single(
            household.Members,
            membership => membership.Role != HouseholdRole.Owner);
        Assert.NotEqual(Guid.Empty, member.MembershipId);
        Assert.NotEqual(Guid.Empty, member.PersonId);
        Assert.Equal(HouseholdRole.Member, member.Role);
    }

    [Fact]
    public void AddMember_allows_multiple_distinct_persons()
    {
        var household = CreateHousehold();

        var firstResult = household.AddMember(household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId, HouseholdRole.Member, Guid.NewGuid());
        var secondResult = household.AddMember(household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId, HouseholdRole.Guest, Guid.NewGuid());

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        Assert.Equal(
            2,
            household.Members.Count(membership => membership.Role != HouseholdRole.Owner));
    }

    [Fact]
    public void AddMember_is_visible_through_an_existing_read_only_members_view()
    {
        var household = CreateHousehold();
        var members = household.Members;
        var personId = Guid.NewGuid();

        var result = household.AddMember(
            household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
            HouseholdRole.Member,
            personId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, members.Count);
        Assert.Contains(
            members,
            membership => membership.PersonId == personId);
    }

    [Fact]
    public void AddMember_fails_when_person_is_already_linked()
    {
        var household = CreateHousehold();
        var personId = Guid.NewGuid();

        var firstResult = household.AddMember(
            household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
            HouseholdRole.Member,
            personId);
        var duplicateResult = household.AddMember(
            household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
            HouseholdRole.Guest,
            personId);

        Assert.True(firstResult.IsSuccess);
        Assert.False(duplicateResult.IsSuccess);
        Assert.Equal(
            AddHouseholdMemberError.PersonAlreadyMember,
            duplicateResult.Error);
        Assert.Equal(2, household.Members.Count);
        Assert.Single(
            household.Members,
            membership => membership.PersonId == personId);
    }

    [Fact]
    public void AddMember_updates_updated_at()
    {
        var household = CreateHousehold();
        var previousUpdatedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        typeof(Domain.Household.Household).GetProperty(nameof(household.UpdatedAt))!
            .SetValue(household, previousUpdatedAt);

        var result = household.AddMember(
            household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
            HouseholdRole.Member,
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.True(household.UpdatedAt > previousUpdatedAt);
        Assert.Equal(DateTimeKind.Utc, household.UpdatedAt.Kind);
    }

    [Fact]
    public void Initial_owner_always_has_a_person()
    {
        var household = CreateHousehold();

        var owner = Assert.Single(
            household.Members,
            membership => membership.Role == HouseholdRole.Owner);
        Assert.NotEqual(Guid.Empty, owner.PersonId);
    }

    [Fact]
    public void AddMember_throws_when_person_id_is_empty()
    {
        var household = CreateHousehold();

        Assert.Throws<ArgumentException>(() =>
            household.AddMember(household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId, HouseholdRole.Member, Guid.Empty));
        Assert.Single(household.Members);
    }

    [Theory]
    [InlineData("outsider", AddHouseholdMemberError.CurrentPersonNotMember)]
    [InlineData("member", AddHouseholdMemberError.CurrentPersonNotOwner)]
    [InlineData("guest", AddHouseholdMemberError.CurrentPersonNotOwner)]
    [InlineData("duplicate", AddHouseholdMemberError.PersonAlreadyMember)]
    public void AddMember_failure_preserves_members_ownership_and_updated_at(
        string scenario, AddHouseholdMemberError expected)
    {
        var household = CreateHousehold();
        var ownerId = Assert.Single(household.Members).PersonId;
        var actorId = ownerId;
        if (scenario is "member" or "guest")
        {
            actorId = Guid.NewGuid();
            Assert.True(household.AddMember(ownerId,
                scenario == "member" ? HouseholdRole.Member : HouseholdRole.Guest,
                actorId).IsSuccess);
        }
        if (scenario == "outsider") actorId = Guid.NewGuid();
        var before = household.Members.Select(member =>
            (member.MembershipId, member.PersonId, member.Role)).ToArray();
        var updatedAt = household.UpdatedAt;

        var result = household.AddMember(actorId,
            HouseholdRole.Member,
            scenario == "duplicate" ? ownerId : Guid.NewGuid());

        Assert.False(result.IsSuccess);
        Assert.Equal(expected, result.Error);
        Assert.Equal(before, household.Members.Select(member =>
            (member.MembershipId, member.PersonId, member.Role)).ToArray());
        Assert.Equal(ownerId, Assert.Single(household.Members,
            member => member.Role == HouseholdRole.Owner).PersonId);
        Assert.Equal(updatedAt, household.UpdatedAt);
    }

    private static Domain.Household.Household CreateHousehold()
    {
        return new Domain.Household.Household(
            "Mit hjem",
            Guid.NewGuid());
    }
}
