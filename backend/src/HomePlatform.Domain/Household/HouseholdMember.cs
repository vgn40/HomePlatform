namespace HomePlatform.Domain.Household;

public class HouseholdMember
{
    private HouseholdMember()
    {
    }

    public Guid MembershipId { get; }
    public Guid PersonId { get; private set; }
    public HouseholdRole Role { get; private set; }

    internal HouseholdMember(
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

        MembershipId = Guid.NewGuid();
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
