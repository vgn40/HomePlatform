using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Tests.Households;

internal sealed class HouseholdSnapshot
{
    private readonly Guid _id;
    private readonly string _name;
    private readonly DateTime _createdAt;
    private readonly DateTime _updatedAt;
    private readonly Guid _ownershipVersion;
    private readonly (Guid MembershipId, Guid HouseholdId, Guid PersonId, HouseholdRole Role)[] _members;

    private HouseholdSnapshot(Household household)
    {
        _id = household.Id;
        _name = household.Name;
        _createdAt = household.CreatedAt;
        _updatedAt = household.UpdatedAt;
        _ownershipVersion = household.OwnershipVersion;
        _members = Members(household);
    }

    public static HouseholdSnapshot Capture(Household household) => new(household);

    public void AssertUnchanged(Household household)
    {
        Assert.Equal(_id, household.Id);
        Assert.Equal(_name, household.Name);
        Assert.Equal(_createdAt, household.CreatedAt);
        Assert.Equal(_updatedAt, household.UpdatedAt);
        Assert.Equal(_ownershipVersion, household.OwnershipVersion);
        Assert.Equal(_members, Members(household));
    }

    private static (Guid MembershipId, Guid HouseholdId, Guid PersonId, HouseholdRole Role)[] Members(
        Household household) => household.Members.OrderBy(member => member.MembershipId)
            .Select(member => (member.MembershipId, member.HouseholdId, member.PersonId, member.Role))
            .ToArray();
}
