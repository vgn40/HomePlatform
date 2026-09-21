using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public class HouseholdMembersTest
{
    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public void AddMember_with_person_succeeds_and_creates_expected_membership(
        HouseholdRole role)
    {
        var household = CreateHousehold();
        var personId = Guid.NewGuid();

        var result = household.AddMember(role, personId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);

        var member = Assert.Single(
            household.Members,
            membership => membership.PersonId == personId);
        Assert.NotEqual(Guid.Empty, member.MembershipId);
        Assert.Equal(personId, member.PersonId);
        Assert.Equal(role, member.Role);
    }

    [Fact]
    public void AddMember_with_another_person_succeeds()
    {
        var household = CreateHousehold();

        var result = household.AddMember(HouseholdRole.Member, Guid.NewGuid());

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

        var firstResult = household.AddMember(HouseholdRole.Member, Guid.NewGuid());
        var secondResult = household.AddMember(HouseholdRole.Guest, Guid.NewGuid());

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
            HouseholdRole.Member,
            personId);
        var duplicateResult = household.AddMember(
            HouseholdRole.Guest,
            personId);

        Assert.True(firstResult.IsSuccess);
        Assert.False(duplicateResult.IsSuccess);
        Assert.Equal(
            "Person is already linked to a membership in this household.",
            duplicateResult.ErrorMessage);
        Assert.Equal(2, household.Members.Count);
        Assert.Single(
            household.Members,
            membership => membership.PersonId == personId);
    }

    [Fact]
    public void AddMember_keeps_updated_at_initialized()
    {
        var household = CreateHousehold();

        var result = household.AddMember(
            HouseholdRole.Member,
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.NotEqual(default, household.UpdatedAt);
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
            household.AddMember(HouseholdRole.Owner, Guid.Empty));
        Assert.Single(household.Members);
    }

    private static Domain.Household.Household CreateHousehold()
    {
        return new Domain.Household.Household(
            "Mit hjem",
            Guid.NewGuid());
    }
}
