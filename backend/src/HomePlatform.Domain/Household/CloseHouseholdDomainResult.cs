namespace HomePlatform.Domain.Household;

public sealed record CloseHouseholdDomainResult
{
    public CloseHouseholdError? Error { get; }

    public bool IsSuccess => Error is null;

    private CloseHouseholdDomainResult(
        CloseHouseholdError? error)
    {
        Error = error;
    }

    public static CloseHouseholdDomainResult Success()
    {
        return new CloseHouseholdDomainResult(
            error: null);
    }

    public static CloseHouseholdDomainResult Failure(
        CloseHouseholdError error)
    {
        return new CloseHouseholdDomainResult(
            error);
    }
}