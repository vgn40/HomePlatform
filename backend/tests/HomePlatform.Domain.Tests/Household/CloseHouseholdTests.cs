using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public sealed class CloseHouseholdTests
{
    [Theory]
    [InlineData("owner", true, null)]
    [InlineData("member", false, CloseHouseholdError.CurrentPersonNotOwner)]
    [InlineData("guest", false, CloseHouseholdError.CurrentPersonNotOwner)]
    [InlineData("nonmember", false, CloseHouseholdError.CurrentPersonNotMember)]
    [InlineData("loginless", false, CloseHouseholdError.CurrentPersonNotMember)]
    [InlineData("empty-person", false, CloseHouseholdError.CurrentPersonNotMember)]
    public void Close_authorizes_only_owner_and_preserves_entire_aggregate(
        string caller,
        bool allowed,
        CloseHouseholdError? expectedError)
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var guestId = Guid.NewGuid();

        var household =
            new Domain.Household.Household(
                "Home",
                ownerId);

        Assert.True(
            household.AddMember(
                household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
                HouseholdRole.Member,
                memberId).IsSuccess);

        Assert.True(
            household.AddMember(
                household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
                HouseholdRole.Guest,
                guestId).IsSuccess);

        Assert.True(
            household.AddMember(
                household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
                HouseholdRole.Member, Guid.NewGuid()).IsSuccess);

        Assert.True(
            household.AddMember(
                household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
                HouseholdRole.Guest, Guid.NewGuid()).IsSuccess);

        var earlier =
            new DateTime(
                2000,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc);

        // Match existing tests: make timestamp mutation observable without sleeping.
        typeof(Domain.Household.Household)
            .GetProperty(nameof(household.UpdatedAt))!
            .SetValue(
                household,
                earlier);

        var metadata =
            (
                household.Id,
                household.Name,
                household.CreatedAt,
                household.UpdatedAt
            );

        var members =
            household.Members.ToArray();

        var values =
            members
                .Select(
                    member =>
                        (
                            member.MembershipId,
                            member.PersonId,
                            member.Role
                        ))
                .ToArray();

        var personId = caller switch
        {
            "owner" => ownerId,
            "member" => memberId,
            "guest" => guestId,

            // Loginless memberships have no PersonId,
            // so their MembershipId cannot authorize an person request.
            "loginless" => household.Members
                .First(
                    member =>
                        member.Role != HouseholdRole.Owner)
                .MembershipId,

            "empty-person" => Guid.Empty,

            _ => Guid.NewGuid()
        };

        var result =
            household.Close(personId);

        Assert.Equal(
            allowed,
            result.IsSuccess);

        Assert.Equal(
            expectedError,
            result.Error);

        Assert.Equal(
            metadata,
            (
                household.Id,
                household.Name,
                household.CreatedAt,
                household.UpdatedAt
            ));

        Assert.Equal(
            members.Length,
            household.Members.Count);

        for (var i = 0; i < members.Length; i++)
        {
            Assert.Same(
                members[i],
                household.Members.ElementAt(i));
        }

        Assert.Equal(
            values,
            household.Members
                .Select(
                    member =>
                        (
                            member.MembershipId,
                            member.PersonId,
                            member.Role
                        ))
                .ToArray());
    }

    [Fact]
    public void Success_has_no_error()
    {
        var result =
            CloseHouseholdDomainResult.Success();

        Assert.True(
            result.IsSuccess);

        Assert.Null(
            result.Error);
    }

    [Theory]
    [InlineData(CloseHouseholdError.CurrentPersonNotOwner)]
    [InlineData(CloseHouseholdError.CurrentPersonNotMember)]
    public void Failure_preserves_exact_error(
        CloseHouseholdError error)
    {
        var result =
            CloseHouseholdDomainResult.Failure(error);

        Assert.False(
            result.IsSuccess);

        Assert.Equal(
            error,
            result.Error);
    }
}
