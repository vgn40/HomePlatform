namespace HomePlatform.Application.Households.GetHousehold;

public enum GetHouseholdOutcome
{
    Success,
    Unauthenticated,
    NotFound
}

public sealed record GetHouseholdResult
{
    public GetHouseholdOutcome Outcome { get; }
    public HouseholdDetailResponse? Household { get; }

    private GetHouseholdResult(
        GetHouseholdOutcome outcome,
        HouseholdDetailResponse? household = null)
    {
        Outcome = outcome;
        Household = household;
    }

    public static GetHouseholdResult Success(HouseholdDetailResponse household)
        => new(GetHouseholdOutcome.Success, household);

    public static GetHouseholdResult Unauthenticated()
        => new(GetHouseholdOutcome.Unauthenticated);

    public static GetHouseholdResult NotFound()
        => new(GetHouseholdOutcome.NotFound);
}
