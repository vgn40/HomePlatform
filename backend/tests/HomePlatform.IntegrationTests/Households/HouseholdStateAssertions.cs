using HomePlatform.Domain.Household;

namespace HomePlatform.IntegrationTests.Households;

internal static class HouseholdStateAssertions
{
    public static void AssertUnchanged(IReadOnlyCollection<Household> before, IReadOnlyCollection<Household> after)
    {
        Assert.Equal(before.Count, after.Count);
        foreach (var original in before)
        {
            var current = Assert.Single(after, household => household.Id == original.Id);
            Assert.Equal(original.Name, current.Name);
            Assert.Equal(original.CreatedAt, current.CreatedAt);
            Assert.Equal(original.UpdatedAt, current.UpdatedAt);
            Assert.Equal(original.OwnershipVersion, current.OwnershipVersion);
            Assert.Equal(Members(original), Members(current));
        }
    }

    private static (Guid MembershipId, Guid HouseholdId, Guid PersonId, HouseholdRole Role)[] Members(
        Household household) => household.Members.OrderBy(member => member.MembershipId)
            .Select(member => (member.MembershipId, member.HouseholdId, member.PersonId, member.Role))
            .ToArray();
}
