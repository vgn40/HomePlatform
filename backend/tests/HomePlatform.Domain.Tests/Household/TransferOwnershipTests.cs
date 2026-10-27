using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public class TransferOwnershipTests
{
    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public void TransferOwnership_succeeds_and_preserves_memberships_and_advances_updated_at(
        HouseholdRole targetRole)
    {
        var ownerPersonId = Guid.NewGuid();
        var household = CreateHousehold(ownerPersonId);
        var target = AddMember(
            household,
            targetRole,
            Guid.NewGuid());

        var owner = Assert.Single(
            household.Members,
            member => member.PersonId == ownerPersonId);

        var ownerMembershipId = owner.MembershipId;
        var targetMembershipId = target.MembershipId;
        var targetPersonId = target.PersonId;
        var memberCount = household.Members.Count;

        var previousUpdatedAt = SetEarlierUpdatedAt(household);
        var beforeTransfer = DateTime.UtcNow;

        TransferOwnershipDomainResult result =
            household.TransferOwnership(
                ownerPersonId,
                targetMembershipId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.True(household.UpdatedAt > previousUpdatedAt);
        Assert.Equal(DateTimeKind.Utc, household.UpdatedAt.Kind);
        Assert.InRange(household.UpdatedAt, beforeTransfer, DateTime.UtcNow);
        Assert.Equal(memberCount, household.Members.Count);

        var updatedOwner = Assert.Single(
            household.Members,
            member => member.MembershipId == ownerMembershipId);

        var updatedTarget = Assert.Single(
            household.Members,
            member => member.MembershipId == targetMembershipId);

        Assert.Equal(
            ownerPersonId,
            updatedOwner.PersonId);

        Assert.Equal(
            targetPersonId,
            updatedTarget.PersonId);

        Assert.Equal(
            HouseholdRole.Member,
            updatedOwner.Role);

        Assert.Equal(
            HouseholdRole.Owner,
            updatedTarget.Role);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public void TransferOwnership_fails_when_caller_is_not_an_owner(
        HouseholdRole callerRole)
    {
        var household =
            CreateHousehold(Guid.NewGuid());

        var callerPersonId =
            Guid.NewGuid();

        AddMember(
            household,
            callerRole,
            callerPersonId);

        var target = AddMember(
            household,
            HouseholdRole.Member,
            Guid.NewGuid());

        var previousUpdatedAt =
            SetEarlierUpdatedAt(household);

        var before =
            SnapshotMembers(household);

        TransferOwnershipDomainResult result =
            household.TransferOwnership(
                callerPersonId,
                target.MembershipId);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            TransferOwnershipError.CurrentPersonNotOwner,
            result.Error);

        Assert.Equal(
            before,
            SnapshotMembers(household));

        Assert.Equal(
            previousUpdatedAt,
            household.UpdatedAt);
    }

    [Fact]
    public void TransferOwnership_fails_when_target_membership_does_not_exist()
    {
        var ownerPersonId =
            Guid.NewGuid();

        var household =
            CreateHousehold(ownerPersonId);

        AddMember(
            household,
            HouseholdRole.Member,
            Guid.NewGuid());

        var missingMembershipId =
            Guid.NewGuid();

        Assert.DoesNotContain(
            household.Members,
            member =>
                member.MembershipId ==
                missingMembershipId);

        var previousUpdatedAt =
            SetEarlierUpdatedAt(household);

        var before =
            SnapshotMembers(household);

        TransferOwnershipDomainResult result =
            household.TransferOwnership(
                ownerPersonId,
                missingMembershipId);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            TransferOwnershipError.NewOwnerNotFound,
            result.Error);

        Assert.Equal(
            before,
            SnapshotMembers(household));

        Assert.Equal(
            previousUpdatedAt,
            household.UpdatedAt);
    }

    [Fact]
    public void TransferOwnership_fails_when_caller_is_not_a_household_member()
    {
        var household =
            CreateHousehold(Guid.NewGuid());

        var target = AddMember(
            household,
            HouseholdRole.Member,
            Guid.NewGuid());

        var outsiderPersonId =
            Guid.NewGuid();

        Assert.DoesNotContain(
            household.Members,
            member =>
                member.PersonId ==
                outsiderPersonId);

        var previousUpdatedAt =
            SetEarlierUpdatedAt(household);

        var before =
            SnapshotMembers(household);

        TransferOwnershipDomainResult result =
            household.TransferOwnership(
                outsiderPersonId,
                target.MembershipId);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            TransferOwnershipError.CurrentPersonNotMember,
            result.Error);

        Assert.Equal(
            before,
            SnapshotMembers(household));

        Assert.Equal(
            previousUpdatedAt,
            household.UpdatedAt);
    }

    [Fact]
    public void TransferOwnership_fails_when_owner_targets_their_own_membership()
    {
        var ownerPersonId =
            Guid.NewGuid();

        var household =
            CreateHousehold(ownerPersonId);

        var owner =
            Assert.Single(household.Members);

        AddMember(
            household,
            HouseholdRole.Member,
            Guid.NewGuid());

        var previousUpdatedAt =
            SetEarlierUpdatedAt(household);

        var before =
            SnapshotMembers(household);

        TransferOwnershipDomainResult result =
            household.TransferOwnership(
                ownerPersonId,
                owner.MembershipId);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            TransferOwnershipError.CannotTransferToSelf,
            result.Error);

        Assert.Equal(
            before,
            SnapshotMembers(household));

        Assert.Equal(
            previousUpdatedAt,
            household.UpdatedAt);
    }

    [Theory]
    [InlineData(
        HouseholdRole.Member,
        false,
        TransferOwnershipError.CurrentPersonNotOwner)]
    [InlineData(
        HouseholdRole.Member,
        true,
        TransferOwnershipError.CurrentPersonNotOwner)]
    [InlineData(
        HouseholdRole.Guest,
        false,
        TransferOwnershipError.CurrentPersonNotOwner)]
    [InlineData(
        HouseholdRole.Guest,
        true,
        TransferOwnershipError.CurrentPersonNotOwner)]
    [InlineData(
        null,
        false,
        TransferOwnershipError.CurrentPersonNotMember)]
    [InlineData(
        null,
        true,
        TransferOwnershipError.CurrentPersonNotMember)]
    public void TransferOwnership_checks_caller_authorization_before_target_lookup(
        HouseholdRole? callerRole,
        bool targetExists,
        TransferOwnershipError expectedError)
    {
        var household =
            CreateHousehold(Guid.NewGuid());

        var callerPersonId =
            Guid.NewGuid();

        if (callerRole is HouseholdRole role)
        {
            AddMember(
                household,
                role,
                callerPersonId);
        }

        var targetId =
            targetExists
                ? AddMember(
                    household,
                    HouseholdRole.Member,
                    Guid.NewGuid()).MembershipId
                : Guid.NewGuid();

        var previousUpdatedAt =
            SetEarlierUpdatedAt(household);

        var before =
            SnapshotMembers(household);

        var result =
            household.TransferOwnership(
                callerPersonId,
                targetId);

        Assert.False(
            result.IsSuccess);

        Assert.Equal(
            expectedError,
            result.Error);

        Assert.Equal(
            before,
            SnapshotMembers(household));

        Assert.Equal(
            previousUpdatedAt,
            household.UpdatedAt);
    }

    private static DateTime SetEarlierUpdatedAt(
        Domain.Household.Household household)
    {
        var previousUpdatedAt =
            new DateTime(
                2000,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

        var property =
            typeof(Domain.Household.Household)
                .GetProperty(
                    nameof(
                        Domain.Household.Household.UpdatedAt));

        Assert.NotNull(
            property);

        property.SetValue(
            household,
            previousUpdatedAt);

        Assert.Equal(
            previousUpdatedAt,
            household.UpdatedAt);

        return previousUpdatedAt;
    }

    private static Domain.Household.Household CreateHousehold(
        Guid ownerPersonId)
    {
        return new Domain.Household.Household(
            "Mit hjem",
            ownerPersonId);
    }

    private static HouseholdMember AddMember(
        Domain.Household.Household household,
        HouseholdRole role,
        Guid personId)
    {
        var result =
            household.AddMember(
                household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
                role,
                personId);

        Assert.True(
            result.IsSuccess);

        return Assert.Single(
            household.Members,
            member =>
                member.PersonId ==
                personId);
    }

    private static (
        Guid MembershipId,
        Guid PersonId,
        HouseholdRole Role
    )[] SnapshotMembers(
        Domain.Household.Household household)
    {
        return household.Members
            .OrderBy(
                member =>
                    member.MembershipId)
            .Select(
                member =>
                    (
                        member.MembershipId,
                        member.PersonId,
                        member.Role
                    ))
            .ToArray();
    }
}
