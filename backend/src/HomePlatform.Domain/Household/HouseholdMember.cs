namespace HomePlatform.Domain.Household;

public class HouseholdMember
{
    public Guid UserId { get; }
    public HouseholdRole Role { get; }
    public HouseholdMember(Guid userId, HouseholdRole role)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentException("Invalid role specified.", nameof(role));
        }

        UserId = userId;
        Role = role;
    }
}