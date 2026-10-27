namespace HomePlatform.Application.Households.RemoveHouseholdMember;

public sealed record RemoveHouseholdMemberResult
{
    public RemoveHouseholdMemberOutcome Outcome { get; }
    public string? ErrorMessage { get; }

    private RemoveHouseholdMemberResult(
        RemoveHouseholdMemberOutcome outcome,
        string? errorMessage = null)
    {
        Outcome = outcome;
        ErrorMessage = errorMessage;
    }

    public static RemoveHouseholdMemberResult Success()
    {
        return new RemoveHouseholdMemberResult(
            RemoveHouseholdMemberOutcome.Success);
    }

    public static RemoveHouseholdMemberResult Unauthenticated()
    {
        return new RemoveHouseholdMemberResult(
            RemoveHouseholdMemberOutcome.Unauthenticated);
    }

    public static RemoveHouseholdMemberResult NotFound()
    {
        return new RemoveHouseholdMemberResult(
            RemoveHouseholdMemberOutcome.NotFound);
    }

    public static RemoveHouseholdMemberResult Forbidden()
    {
        return new RemoveHouseholdMemberResult(
            RemoveHouseholdMemberOutcome.Forbidden);
    }

    public static RemoveHouseholdMemberResult Invalid(
        string errorMessage)
    {
        return new RemoveHouseholdMemberResult(
            RemoveHouseholdMemberOutcome.Invalid,
            errorMessage);
    }
}
