namespace HomePlatform.Domain.Household;

public class Household
{
    public const int MaxNameLength = 100;

    private readonly List<HouseholdMember> _members = new();

    public Guid Id { get; }
    public string Name { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime UpdatedAt { get; private set; }
    public Guid OwnershipVersion { get; private set; }

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
        OwnershipVersion = Guid.NewGuid();

        var owner = new HouseholdMember(
            HouseholdRole.Owner,
            ownerPersonId);

        _members.Add(owner);
    }

    public AddHouseholdMemberDomainResult AddMember(
        Guid currentPersonId,
        HouseholdRole role,
        Guid personId)
    {
        var currentMember = _members.FirstOrDefault(
            member =>
                member.PersonId == currentPersonId);

        if (currentMember is null)
        {
            return AddHouseholdMemberDomainResult.Failure(
                AddHouseholdMemberError.CurrentPersonNotMember);
        }

        if (currentMember.Role != HouseholdRole.Owner)
        {
            return AddHouseholdMemberDomainResult.Failure(
                AddHouseholdMemberError.CurrentPersonNotOwner);
        }

        if (_members.Any(
                member =>
                    member.PersonId == personId))
        {
            return AddHouseholdMemberDomainResult.Failure(
                AddHouseholdMemberError.PersonAlreadyMember);
        }

        var member = new HouseholdMember(
            role,
            personId);

        _members.Add(member);

        if (role == HouseholdRole.Owner)
        {
            MarkOwnershipUpdated();
        }
        else
        {
            MarkUpdated();
        }

        return AddHouseholdMemberDomainResult.Success();
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

        if (newOwner.Role == HouseholdRole.Owner)
        {
            return TransferOwnershipDomainResult.Failure(
                TransferOwnershipError.NewOwnerAlreadyOwner);
        }

        return TransferOwnershipDomainResult.Success();
    }

    public TransferOwnershipDomainResult TransferOwnership(
        Guid currentOwnerPersonId,
        Guid newOwnerMembershipId)
    {
        var validation = ValidateTransferOwnership(
            currentOwnerPersonId,
            newOwnerMembershipId);

        if (!validation.IsSuccess)
        {
            return validation;
        }

        var currentMember = _members.Single(
            member =>
                member.PersonId == currentOwnerPersonId);

        var newOwner = _members.Single(
            member =>
                member.MembershipId == newOwnerMembershipId);

        newOwner.ChangeRole(HouseholdRole.Owner);
        currentMember.ChangeRole(HouseholdRole.Member);
        MarkOwnershipUpdated();

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

        var isOwner = currentMember.Role == HouseholdRole.Owner;

        if (isOwner &&
            !_members.Any(member =>
                member.PersonId != currentPersonId &&
                member.Role == HouseholdRole.Owner))
        {
            return LeaveHouseholdDomainResult.Failure(
                LeaveHouseholdError.LastOwnerCannotLeave);
        }

        _members.Remove(currentMember);

        if (isOwner)
        {
            MarkOwnershipUpdated();
        }
        else
        {
            MarkUpdated();
        }

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

    private void MarkUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    private void MarkOwnershipUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
        OwnershipVersion = Guid.NewGuid();
    }
}