namespace HomePlatform.Application.Households.AddHouseholdMemberWithoutAccount;

public sealed record AddHouseholdMemberWithoutAccountResult(
    AddHouseholdMemberWithoutAccountOutcome Outcome,
    Guid? PersonId = null,
    Guid? MembershipId = null,
    string? Error = null)
{
    public static AddHouseholdMemberWithoutAccountResult Success(
        Guid personId,
        Guid membershipId)
        => new(
            AddHouseholdMemberWithoutAccountOutcome.Success,
            personId,
            membershipId);

    public static AddHouseholdMemberWithoutAccountResult Unauthenticated()
        => new(AddHouseholdMemberWithoutAccountOutcome.Unauthenticated);

    public static AddHouseholdMemberWithoutAccountResult NotFound()
        => new(AddHouseholdMemberWithoutAccountOutcome.NotFound);

    public static AddHouseholdMemberWithoutAccountResult Forbidden()
        => new(AddHouseholdMemberWithoutAccountOutcome.Forbidden);

    public static AddHouseholdMemberWithoutAccountResult Invalid(string error)
        => new(
            AddHouseholdMemberWithoutAccountOutcome.Invalid,
            Error: error);
}