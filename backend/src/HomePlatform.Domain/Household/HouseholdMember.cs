namespace HomePlatform.Domain.Household;

public class HouseholdMember
{
    private HouseholdMember()
    {
    }

    public Guid MembershipId { get; }
    public Guid? AccountId { get; private set; }
    public HouseholdRole Role { get; private set; }

    internal HouseholdMember(
        HouseholdRole role,
        Guid? accountId = null)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentException(
                "Invalid role specified.",
                nameof(role));
        }

        if (accountId == Guid.Empty)
        {
            throw new ArgumentException(
                "Account ID cannot be empty.",
                nameof(accountId));
        }

        if (role == HouseholdRole.Owner && accountId is null)
        {
            throw new ArgumentException(
                "An owner must be linked to an account.",
                nameof(accountId));
        }

        MembershipId = Guid.NewGuid();
        AccountId = accountId;
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

        if (role == HouseholdRole.Owner && AccountId is null)
        {
            throw new InvalidOperationException(
                "An owner must be linked to an account.");
        }

        Role = role;
    }
}