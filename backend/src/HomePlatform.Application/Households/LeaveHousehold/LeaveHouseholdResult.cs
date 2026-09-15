namespace HomePlatform.Application.Households.LeaveHousehold;

public sealed record LeaveHouseholdResult
{
    public LeaveHouseholdOutcome Outcome { get; }
    public string? ErrorMessage { get; }

    private LeaveHouseholdResult(
        LeaveHouseholdOutcome outcome,
        string? errorMessage = null)
    {
        Outcome = outcome;
        ErrorMessage = errorMessage;
    }

    public static LeaveHouseholdResult Success()
    {
        return new LeaveHouseholdResult(
            LeaveHouseholdOutcome.Success);
    }

    public static LeaveHouseholdResult Unauthenticated()
    {
        return new LeaveHouseholdResult(
            LeaveHouseholdOutcome.Unauthenticated);
    }

    public static LeaveHouseholdResult NotFound()
    {
        return new LeaveHouseholdResult(
            LeaveHouseholdOutcome.NotFound);
    }

    public static LeaveHouseholdResult Forbidden()
    {
        return new LeaveHouseholdResult(
            LeaveHouseholdOutcome.Forbidden);
    }

    public static LeaveHouseholdResult Invalid(
        string errorMessage)
    {
        return new LeaveHouseholdResult(
            LeaveHouseholdOutcome.Invalid,
            errorMessage);
    }
}