namespace HomePlatform.Domain.Household;

public sealed record LeaveHouseholdDomainResult
{
    public LeaveHouseholdError? Error { get; }

    public bool IsSuccess => Error is null;

    private LeaveHouseholdDomainResult(
        LeaveHouseholdError? error)
    {
        Error = error;
    }

    public static LeaveHouseholdDomainResult Success()
    {
        return new LeaveHouseholdDomainResult(
            error: null);
    }

    public static LeaveHouseholdDomainResult Failure(
        LeaveHouseholdError error)
    {
        return new LeaveHouseholdDomainResult(
            error);
    }
}