using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public class HouseholdMembersTest
{
    [Fact]
    public void AddMember_adds_member()
    {
        var household = new Domain.Household.Household("Mit hjem");
        var member = new HouseholdMember(
            Guid.NewGuid(),
            HouseholdRole.Parent);

        var result = household.AddMember(member);

        Assert.True(result.IsSuccess);
        Assert.Single(household.Members);
        Assert.Contains(member, household.Members);
    }

    [Fact]
    public void AddMember_fails_when_user_is_already_member()
    {
        var household = new Domain.Household.Household("Mit hjem");
        var userId = Guid.NewGuid();

        var firstMember = new HouseholdMember(
            userId,
            HouseholdRole.Parent);

        var duplicateMember = new HouseholdMember(
            userId,
            HouseholdRole.Guest);

        household.AddMember(firstMember);

        var result = household.AddMember(duplicateMember);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "User is already a member of the household.",
            result.ErrorMessage);

        Assert.Single(household.Members);
    }

    [Fact]
    public void AddMember_throws_when_member_is_null()
    {
        var household = new Domain.Household.Household("Mit hjem");

        Assert.Throws<ArgumentNullException>(
            () => household.AddMember(null!));
    }

    [Fact]
    public void AddMember_updates_updated_at()
    {
        var household = new Domain.Household.Household("Mit hjem");
        var originalUpdatedAt = household.UpdatedAt;

        Thread.Sleep(1);

        var member = new HouseholdMember(
            Guid.NewGuid(),
            HouseholdRole.Parent);

        household.AddMember(member);

        Assert.True(household.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void HouseholdMember_throws_when_user_id_is_empty()
    {
        Assert.Throws<ArgumentException>(() =>
            new HouseholdMember(
                Guid.Empty,
                HouseholdRole.Parent));
    }

    [Fact]
    public void HouseholdMember_stores_user_id_and_role()
    {
        var userId = Guid.NewGuid();

        var member = new HouseholdMember(
            userId,
            HouseholdRole.Child);

        Assert.Equal(userId, member.UserId);
        Assert.Equal(HouseholdRole.Child, member.Role);
    }
}