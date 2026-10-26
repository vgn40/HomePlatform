namespace HomePlatform.Application.Households.ListHouseholds;

public enum ListHouseholdsOutcome
{
    Success,
    Unauthenticated
}

public sealed record ListHouseholdsResult
{
    public ListHouseholdsOutcome Outcome { get; }
    public IReadOnlyList<HouseholdSummaryResponse>? Households { get; }

    private ListHouseholdsResult(
        ListHouseholdsOutcome outcome,
        IReadOnlyList<HouseholdSummaryResponse>? households = null)
    {
        Outcome = outcome;
        Households = households;
    }

    public static ListHouseholdsResult Success(
        IReadOnlyList<HouseholdSummaryResponse> households)
        => new(ListHouseholdsOutcome.Success, households);

    public static ListHouseholdsResult Unauthenticated()
        => new(ListHouseholdsOutcome.Unauthenticated);
}
