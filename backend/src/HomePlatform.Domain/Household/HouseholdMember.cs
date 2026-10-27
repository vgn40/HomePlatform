namespace HomePlatform.Domain.Household;

public class HouseholdMember
{
    private HouseholdMember()
    {
    }

    public Guid MembershipId { get; }
    public Guid HouseholdId { get; private set; }
    public Guid PersonId { get; private set; }
    public HouseholdRole Role { get; private set; }

    internal HouseholdMember(
        Guid householdId,
        HouseholdRole role,
        Guid personId)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentException(
                "Invalid role specified.",
                nameof(role));
        }

        if (personId == Guid.Empty)
        {
            throw new ArgumentException(
                "Person ID cannot be empty.",
                nameof(personId));
        }

        if (householdId == Guid.Empty)
        {
            throw new ArgumentException(
                "Household ID cannot be empty.",
                nameof(householdId));
        }

        MembershipId = Guid.NewGuid();
        HouseholdId = householdId;
        PersonId = personId;
        Role = role;
    }

    internal void ChangeRole(HouseholdRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentException(
                "Invalid role specified.",
                nameof(role));
        }

        Role = role;
    }
}
