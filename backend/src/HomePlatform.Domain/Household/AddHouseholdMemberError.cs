namespace HomePlatform.Domain.Household;

public enum AddHouseholdMemberError
{
    CurrentPersonNotMember,
    CurrentPersonNotOwner,
    CannotAddOwner,
    PersonAlreadyMember
}