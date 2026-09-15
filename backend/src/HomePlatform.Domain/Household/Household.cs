namespace HomePlatform.Domain.Household;

using HomePlatform.Domain.Common;

public class Household
{
    public const int MaxNameLength = 100;

    private readonly List<HouseholdMember> _members = new();

    public Guid Id { get; }
    public string Name { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<HouseholdMember> Members => _members.AsReadOnly();

    private Household()
    {
        Name = null!;
    }

    public Household(string name, Guid ownerAccountId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Household name cannot be empty.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Household name cannot exceed {MaxNameLength} characters.",
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
        Name = normalizedName;
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
            _members.Any(member => member.AccountId == id))
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
    public TransferOwnershipDomainResult TransferOwnership(
        Guid currentOwnerAccountId,
        Guid newOwnerMembershipId)
    {
        var currentOwner = _members.FirstOrDefault(
            member =>
                member.AccountId == currentOwnerAccountId &&
                member.Role == HouseholdRole.Owner);

        if (currentOwner is null)
        {
            return TransferOwnershipDomainResult.Failure(
                TransferOwnershipError.CurrentAccountNotOwner);
        }

        var newOwner = _members.FirstOrDefault(
            member => member.MembershipId == newOwnerMembershipId);

        if (newOwner is null)
        {
            return TransferOwnershipDomainResult.Failure(
                TransferOwnershipError.NewOwnerNotFound);
        }

        if (newOwner.MembershipId == currentOwner.MembershipId)
        {
            return TransferOwnershipDomainResult.Failure(
                TransferOwnershipError.CannotTransferToSelf);
        }

        if (newOwner.AccountId is null)
        {
            return TransferOwnershipDomainResult.Failure(
                TransferOwnershipError.NewOwnerHasNoAccount);
        }

        newOwner.ChangeRole(HouseholdRole.Owner);
        currentOwner.ChangeRole(HouseholdRole.Member);

        UpdatedAt = DateTime.UtcNow;

        return TransferOwnershipDomainResult.Success();
    }
}