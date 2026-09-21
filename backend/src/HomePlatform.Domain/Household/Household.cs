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

    public IReadOnlyCollection<HouseholdMember> Members =>
        _members.AsReadOnly();

    private Household()
    {
        Name = null!;
    }

    public Household(
        string name,
        Guid ownerPersonId)
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

        if (ownerPersonId == Guid.Empty)
        {
            throw new ArgumentException(
                "Owner person ID cannot be empty.",
                nameof(ownerPersonId));
        }

        var now = DateTime.UtcNow;

        Id = Guid.NewGuid();
        Name = normalizedName;
        CreatedAt = now;
        UpdatedAt = now;

        var owner = new HouseholdMember(
            HouseholdRole.Owner,
            ownerPersonId);

        _members.Add(owner);
    }

    public Result AddMember(
        HouseholdRole role,
        Guid personId)
    {
        if (_members.Any(member => member.PersonId == personId))
        {
            return Result.Failure(
                "Person is already linked to a membership in this household.");
        }

        var member = new HouseholdMember(
            role,
            personId);

        _members.Add(member);
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public TransferOwnershipDomainResult ValidateTransferOwnership(
        Guid currentOwnerPersonId,
        Guid newOwnerMembershipId)
    {
        var currentMember = _members.FirstOrDefault(
            member =>
                member.PersonId == currentOwnerPersonId);

        if (currentMember is null)
        {
            return TransferOwnershipDomainResult.Failure(
                TransferOwnershipError.CurrentPersonNotMember);
        }

        if (currentMember.Role != HouseholdRole.Owner)
        {
            return TransferOwnershipDomainResult.Failure(
                TransferOwnershipError.CurrentPersonNotOwner);
        }

        var newOwner = _members.FirstOrDefault(
            member =>
                member.MembershipId == newOwnerMembershipId);

        if (newOwner is null)
        {
            return TransferOwnershipDomainResult.Failure(
                TransferOwnershipError.NewOwnerNotFound);
        }

        if (newOwner.MembershipId == currentMember.MembershipId)
        {
            return TransferOwnershipDomainResult.Failure(
                TransferOwnershipError.CannotTransferToSelf);
        }

        return TransferOwnershipDomainResult.Success();
    }

    public TransferOwnershipDomainResult TransferOwnership(
        Guid currentOwnerPersonId,
        Guid newOwnerMembershipId)
    {
        var validation = ValidateTransferOwnership(currentOwnerPersonId, newOwnerMembershipId);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var currentMember = _members.Single(member => member.PersonId == currentOwnerPersonId);
        var newOwner = _members.Single(member => member.MembershipId == newOwnerMembershipId);
        newOwner.ChangeRole(HouseholdRole.Owner);
        currentMember.ChangeRole(HouseholdRole.Member);
        UpdatedAt = DateTime.UtcNow;
        return TransferOwnershipDomainResult.Success();
    }

    public LeaveHouseholdDomainResult Leave(
        Guid currentPersonId)
    {
        var currentMember = _members.FirstOrDefault(
            member =>
                member.PersonId == currentPersonId);

        if (currentMember is null)
        {
            return LeaveHouseholdDomainResult.Failure(
                LeaveHouseholdError.CurrentPersonNotMember);
        }

        if (currentMember.Role == HouseholdRole.Owner)
        {
            return LeaveHouseholdDomainResult.Failure(
                LeaveHouseholdError.OwnerCannotLeave);
        }

        _members.Remove(currentMember);
        UpdatedAt = DateTime.UtcNow;

        return LeaveHouseholdDomainResult.Success();
    }

    public CloseHouseholdDomainResult Close(
        Guid currentPersonId)
    {
        var currentMember = _members.FirstOrDefault(
            member =>
                member.PersonId == currentPersonId);

        if (currentMember is null)
        {
            return CloseHouseholdDomainResult.Failure(
                CloseHouseholdError.CurrentPersonNotMember);
        }

        if (currentMember.Role != HouseholdRole.Owner)
        {
            return CloseHouseholdDomainResult.Failure(
                CloseHouseholdError.CurrentPersonNotOwner);
        }

        return CloseHouseholdDomainResult.Success();
    }
}
