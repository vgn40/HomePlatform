namespace HomePlatform.Domain.Household;

public enum RemoveHouseholdMemberError
{
    CurrentPersonNotMember,
    CurrentPersonNotOwner,
    MembershipNotFound,
    CannotRemoveSelf,
    CannotRemoveOwner
}
