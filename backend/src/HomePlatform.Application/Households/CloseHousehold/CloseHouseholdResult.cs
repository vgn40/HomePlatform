namespace HomePlatform.Application.Households.CloseHousehold;

public sealed record CloseHouseholdResult
{
    public CloseHouseholdOutcome Outcome { get; }

    private CloseHouseholdResult(
        CloseHouseholdOutcome outcome)
    {
        Outcome = outcome;
    }

    public static CloseHouseholdResult Success()
    {
        return new CloseHouseholdResult(
            CloseHouseholdOutcome.Success);
    }

    public static CloseHouseholdResult Unauthenticated()
    {
        return new CloseHouseholdResult(
            CloseHouseholdOutcome.Unauthenticated);
    }

    public static CloseHouseholdResult NotFound()
    {
        return new CloseHouseholdResult(
            CloseHouseholdOutcome.NotFound);
    }

    public static CloseHouseholdResult Forbidden()
    {
        return new CloseHouseholdResult(
            CloseHouseholdOutcome.Forbidden);
    }
}