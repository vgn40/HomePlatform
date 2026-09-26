namespace HomePlatform.Domain.Household;

public class HouseholdInvitation
{
    public Guid Id { get; }
    public Guid HouseholdId { get; }
    public Guid InvitedPersonId { get; }
    public HouseholdRole Role { get; }
    public DateTime CreatedAt { get; }
    public DateTime ExpiresAt { get; }
    public bool IsAccepted { get; private set; }

    private HouseholdInvitation()
    {
    }

    public HouseholdInvitation(
        Guid householdId,
        Guid invitedPersonId,
        HouseholdRole role,
        DateTime expiresAt)
    {
        if (householdId == Guid.Empty)
        {
            throw new ArgumentException(
                "Household ID cannot be empty.",
                nameof(householdId));
        }

        if (invitedPersonId == Guid.Empty)
        {
            throw new ArgumentException(
                "Invited person ID cannot be empty.",
                nameof(invitedPersonId));
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentException(
                "Invalid role specified.",
                nameof(role));
        }

        if (role == HouseholdRole.Owner)
        {
            throw new ArgumentException(
                "A household invitation cannot grant the Owner role.",
                nameof(role));
        }

        var now = DateTime.UtcNow;

        if (expiresAt <= now)
        {
            throw new ArgumentException(
                "Invitation expiration must be in the future.",
                nameof(expiresAt));
        }

        Id = Guid.NewGuid();
        HouseholdId = householdId;
        InvitedPersonId = invitedPersonId;
        Role = role;
        CreatedAt = now;
        ExpiresAt = expiresAt;
        IsAccepted = false;
    }
}