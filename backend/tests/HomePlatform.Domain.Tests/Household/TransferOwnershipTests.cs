using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public class TransferOwnershipTests
{
    [Fact]
    public void TransferOwnership_to_person_linked_member_succeeds_and_preserves_memberships()
    {
        var ownerPersonId = Guid.NewGuid();
        var household = CreateHousehold(ownerPersonId);
        var target = AddMember(
            household,
            HouseholdRole.Member,
            Guid.NewGuid());

        var owner = Assert.Single(
            household.Members,
            member => member.PersonId == ownerPersonId);

        var ownerMembershipId = owner.MembershipId;
        var targetMembershipId = target.MembershipId;
        var targetPersonId = target.PersonId;
        var memberCount = household.Members.Count;

        TransferOwnershipDomainResult result =
            household.TransferOwnership(
                ownerPersonId,
                targetMembershipId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
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

    [Fact]
    public void TransferOwnership_to_person_linked_guest_succeeds()
    {
        var ownerPersonId = Guid.NewGuid();
        var household = CreateHousehold(ownerPersonId);

        var target = AddMember(
            household,
            HouseholdRole.Guest,
            Guid.NewGuid());

        TransferOwnershipDomainResult result =
            household.TransferOwnership(
                ownerPersonId,
                target.MembershipId);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);

        Assert.Equal(
            HouseholdRole.Owner,
            Assert.Single(
                household.Members,
                member =>
                    member.MembershipId ==
                    target.MembershipId).Role);

        Assert.Equal(
            HouseholdRole.Member,
            Assert.Single(
                household.Members,
                member =>
                    member.PersonId ==
                    ownerPersonId).Role);
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
    public void TransferOwnership_domain_does_not_depend_on_accounts()
    {
        var ownerId = Guid.NewGuid();
        var household = CreateHousehold(ownerId);
        var target = AddMember(household, HouseholdRole.Member, Guid.NewGuid());
        Assert.True(household.TransferOwnership(ownerId, target.MembershipId).IsSuccess);
        Assert.Equal(HouseholdRole.Owner, target.Role);
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

    [Fact]
    public void TransferOwnership_updates_household_updated_at_on_success()
    {
        var ownerPersonId =
            Guid.NewGuid();

        var household =
            CreateHousehold(ownerPersonId);

        var target = AddMember(
            household,
            HouseholdRole.Member,
            Guid.NewGuid());

        var previousUpdatedAt =
            new DateTime(
                2000,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

        // Seed an older timestamp without sleeping or adding a production clock API.
        var updatedAtProperty =
            typeof(Domain.Household.Household)
                .GetProperty(
                    nameof(
                        Domain.Household.Household.UpdatedAt));

        Assert.NotNull(
            updatedAtProperty);

        updatedAtProperty.SetValue(
            household,
            previousUpdatedAt);

        Assert.Equal(
            previousUpdatedAt,
            household.UpdatedAt);

        var beforeTransfer =
            DateTime.UtcNow;

        TransferOwnershipDomainResult result =
            household.TransferOwnership(
                ownerPersonId,
                target.MembershipId);

        var afterTransfer =
            DateTime.UtcNow;

        Assert.True(result.IsSuccess);

        Assert.True(
            household.UpdatedAt >
            previousUpdatedAt);

        Assert.Equal(
            DateTimeKind.Utc,
            household.UpdatedAt.Kind);

        Assert.InRange(
            household.UpdatedAt,
            beforeTransfer,
            afterTransfer);
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
    public void TransferOwnership_checks_caller_authorization_before_invalid_target(
        HouseholdRole? callerRole,
        bool loginlessTarget,
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
            loginlessTarget
                ? AddMember(
                    household,
                    HouseholdRole.Member,
                    null).MembershipId
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
        Guid? personId)
    {
        personId ??= Guid.NewGuid();
        var result =
            household.AddMember(
                role,
                personId.Value);

        Assert.True(
            result.IsSuccess);

        return Assert.Single(
            household.Members,
            member =>
                member.PersonId ==
                personId.Value);
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
