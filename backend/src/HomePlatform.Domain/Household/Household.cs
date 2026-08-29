namespace HomePlatform.Domain.Household;

using HomePlatform.Domain.Common;

public class Household
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<HouseholdMember> _members = new();

    public IReadOnlyCollection<HouseholdMember> Members => _members;

    public Household(string name, Guid ownerAccountId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Household name cannot be empty.",
                nameof(name));
        }

        if (ownerAccountId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner account ID cannot be empty.",
                nameof(ownerAccountId));
        }

        var now = DateTime.UtcNow;

        Id = Guid.NewGuid();
        Name = name.Trim();
        CreatedAt = now;
        UpdatedAt = now;

        var owner = new HouseholdMember(
            HouseholdRole.Owner,
            ownerAccountId);

        _members.Add(owner);
    }

    public Result AddMember(
        HouseholdRole role,
        Guid? accountId = null)
    {
        if (accountId is Guid id &&
            _members.Any(m => m.AccountId == id))
        {
            return Result.Failure(
                "Account is already linked to a membership in this household.");
        }

        var member = new HouseholdMember(
            role,
            accountId);

        _members.Add(member);
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }
}