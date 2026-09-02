using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public class HouseholdMembersTest
{
    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public void AddMember_with_account_succeeds_and_creates_expected_membership(
        HouseholdRole role)
    {
        var household = CreateHousehold();
        var accountId = Guid.NewGuid();

        var result = household.AddMember(role, accountId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);

        var member = Assert.Single(
            household.Members,
            membership => membership.AccountId == accountId);
        Assert.NotEqual(Guid.Empty, member.MembershipId);
        Assert.Equal(accountId, member.AccountId);
        Assert.Equal(role, member.Role);
    }

    [Fact]
    public void AddMember_without_account_succeeds()
    {
        var household = CreateHousehold();

        var result = household.AddMember(HouseholdRole.Member);

        Assert.True(result.IsSuccess);

        var member = Assert.Single(
            household.Members,
            membership => membership.AccountId is null);
        Assert.NotEqual(Guid.Empty, member.MembershipId);
        Assert.Null(member.AccountId);
        Assert.Equal(HouseholdRole.Member, member.Role);
    }

    [Fact]
    public void AddMember_allows_multiple_memberships_without_accounts()
    {
        var household = CreateHousehold();

        var firstResult = household.AddMember(HouseholdRole.Member);
        var secondResult = household.AddMember(HouseholdRole.Guest);

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);
        Assert.Equal(
            2,
            household.Members.Count(membership => membership.AccountId is null));
    }

    [Fact]
    public void AddMember_is_visible_through_an_existing_read_only_members_view()
    {
        var household = CreateHousehold();
        var members = household.Members;
        var accountId = Guid.NewGuid();

        var result = household.AddMember(
            HouseholdRole.Member,
            accountId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, members.Count);
        Assert.Contains(
            members,
            membership => membership.AccountId == accountId);
    }

    [Fact]
    public void AddMember_fails_when_account_is_already_linked()
    {
        var household = CreateHousehold();
        var accountId = Guid.NewGuid();

        var firstResult = household.AddMember(
            HouseholdRole.Member,
            accountId);
        var duplicateResult = household.AddMember(
            HouseholdRole.Guest,
            accountId);

        Assert.True(firstResult.IsSuccess);
        Assert.False(duplicateResult.IsSuccess);
        Assert.Equal(
            "Account is already linked to a membership in this household.",
            duplicateResult.ErrorMessage);
        Assert.Equal(2, household.Members.Count);
        Assert.Single(
            household.Members,
            membership => membership.AccountId == accountId);
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
    public void Initial_owner_always_has_an_account()
    {
        var household = CreateHousehold();

        var owner = Assert.Single(
            household.Members,
            membership => membership.Role == HouseholdRole.Owner);
        Assert.NotNull(owner.AccountId);
    }

    [Fact]
    public void AddMember_throws_when_owner_has_no_account()
    {
        var household = CreateHousehold();

        Assert.Throws<ArgumentException>(() =>
            household.AddMember(HouseholdRole.Owner));
        Assert.Single(household.Members);
    }

    private static Domain.Household.Household CreateHousehold()
    {
        return new Domain.Household.Household(
            "Mit hjem",
            Guid.NewGuid());
    }
}
