using HomePlatform.Domain.Common;
using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public class TransferOwnershipTests
{
    [Fact]
    public void TransferOwnership_to_account_linked_member_succeeds_and_preserves_memberships()
    {
        var ownerAccountId = Guid.NewGuid();
        var household = CreateHousehold(ownerAccountId);
        var target = AddMember(household, HouseholdRole.Member, Guid.NewGuid());
        var owner = Assert.Single(household.Members, member => member.AccountId == ownerAccountId);
        var ownerMembershipId = owner.MembershipId;
        var targetMembershipId = target.MembershipId;
        var targetAccountId = target.AccountId;
        var memberCount = household.Members.Count;

        Result result = household.TransferOwnership(ownerAccountId, targetMembershipId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(memberCount, household.Members.Count);
        var updatedOwner = Assert.Single(household.Members, member => member.MembershipId == ownerMembershipId);
        var updatedTarget = Assert.Single(household.Members, member => member.MembershipId == targetMembershipId);
        Assert.Equal(ownerAccountId, updatedOwner.AccountId);
        Assert.Equal(targetAccountId, updatedTarget.AccountId);
        Assert.Equal(HouseholdRole.Member, updatedOwner.Role);
        Assert.Equal(HouseholdRole.Owner, updatedTarget.Role);
    }

    [Fact]
    public void TransferOwnership_to_account_linked_guest_succeeds()
    {
        var ownerAccountId = Guid.NewGuid();
        var household = CreateHousehold(ownerAccountId);
        var target = AddMember(household, HouseholdRole.Guest, Guid.NewGuid());

        Result result = household.TransferOwnership(ownerAccountId, target.MembershipId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(HouseholdRole.Owner,
            Assert.Single(household.Members, member => member.MembershipId == target.MembershipId).Role);
        Assert.Equal(HouseholdRole.Member,
            Assert.Single(household.Members, member => member.AccountId == ownerAccountId).Role);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public void TransferOwnership_fails_when_caller_is_not_an_owner(HouseholdRole callerRole)
    {
        var household = CreateHousehold(Guid.NewGuid());
        var callerAccountId = Guid.NewGuid();
        AddMember(household, callerRole, callerAccountId);
        var target = AddMember(household, HouseholdRole.Member, Guid.NewGuid());
        var before = SnapshotMembers(household);

        Result result = household.TransferOwnership(callerAccountId, target.MembershipId);

        Assert.False(result.IsSuccess);
        Assert.Equal(before, SnapshotMembers(household));
    }

    [Fact]
    public void TransferOwnership_fails_when_target_membership_does_not_exist()
    {
        var ownerAccountId = Guid.NewGuid();
        var household = CreateHousehold(ownerAccountId);
        AddMember(household, HouseholdRole.Member, Guid.NewGuid());
        var missingMembershipId = Guid.NewGuid();
        Assert.DoesNotContain(household.Members, member => member.MembershipId == missingMembershipId);
        var before = SnapshotMembers(household);

        Result result = household.TransferOwnership(ownerAccountId, missingMembershipId);

        Assert.False(result.IsSuccess);
        Assert.Equal(before, SnapshotMembers(household));
    }

    [Fact]
    public void TransferOwnership_fails_when_target_has_no_account()
    {
        var ownerAccountId = Guid.NewGuid();
        var household = CreateHousehold(ownerAccountId);
        var target = AddMember(household, HouseholdRole.Member, null);
        Assert.Null(target.AccountId);
        var before = SnapshotMembers(household);

        Result result = household.TransferOwnership(ownerAccountId, target.MembershipId);

        Assert.False(result.IsSuccess);
        Assert.Equal(before, SnapshotMembers(household));
    }

    [Fact]
    public void TransferOwnership_fails_when_caller_is_not_a_household_member()
    {
        var household = CreateHousehold(Guid.NewGuid());
        var target = AddMember(household, HouseholdRole.Member, Guid.NewGuid());
        var outsiderAccountId = Guid.NewGuid();
        Assert.DoesNotContain(household.Members, member => member.AccountId == outsiderAccountId);
        var before = SnapshotMembers(household);

        Result result = household.TransferOwnership(outsiderAccountId, target.MembershipId);

        Assert.False(result.IsSuccess);
        Assert.Equal(before, SnapshotMembers(household));
    }

    [Fact]
    public void TransferOwnership_fails_when_owner_targets_their_own_membership()
    {
        var ownerAccountId = Guid.NewGuid();
        var household = CreateHousehold(ownerAccountId);
        var owner = Assert.Single(household.Members);
        AddMember(household, HouseholdRole.Member, Guid.NewGuid());
        var before = SnapshotMembers(household);

        Result result = household.TransferOwnership(ownerAccountId, owner.MembershipId);

        Assert.False(result.IsSuccess);
        Assert.Equal(before, SnapshotMembers(household));
    }

    [Fact]
    public void TransferOwnership_updates_household_updated_at_on_success()
    {
        var ownerAccountId = Guid.NewGuid();
        var household = CreateHousehold(ownerAccountId);
        var target = AddMember(household, HouseholdRole.Member, Guid.NewGuid());
        var previousUpdatedAt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        // Seed an older timestamp without sleeping or adding a production clock API.
        var updatedAtProperty = typeof(Domain.Household.Household)
            .GetProperty(nameof(Domain.Household.Household.UpdatedAt));
        Assert.NotNull(updatedAtProperty);
        updatedAtProperty.SetValue(household, previousUpdatedAt);
        Assert.Equal(previousUpdatedAt, household.UpdatedAt);
        var beforeTransfer = DateTime.UtcNow;

        Result result = household.TransferOwnership(ownerAccountId, target.MembershipId);

        var afterTransfer = DateTime.UtcNow;
        Assert.True(result.IsSuccess);
        Assert.True(household.UpdatedAt > previousUpdatedAt);
        Assert.Equal(DateTimeKind.Utc, household.UpdatedAt.Kind);
        Assert.InRange(household.UpdatedAt, beforeTransfer, afterTransfer);
    }

    private static Domain.Household.Household CreateHousehold(Guid ownerAccountId)
    {
        return new Domain.Household.Household("Mit hjem", ownerAccountId);
    }

    private static HouseholdMember AddMember(
        Domain.Household.Household household,
        HouseholdRole role,
        Guid? accountId)
    {
        var result = household.AddMember(role, accountId);
        Assert.True(result.IsSuccess);
        return Assert.Single(household.Members, member => member.AccountId == accountId);
    }

    private static (Guid MembershipId, Guid? AccountId, HouseholdRole Role)[] SnapshotMembers(
        Domain.Household.Household household)
    {
        return household.Members
            .OrderBy(member => member.MembershipId)
            .Select(member => (member.MembershipId, member.AccountId, member.Role))
            .ToArray();
    }
}
