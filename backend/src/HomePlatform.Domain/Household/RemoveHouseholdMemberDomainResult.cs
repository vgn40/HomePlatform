namespace HomePlatform.Domain.Household;

public sealed record RemoveHouseholdMemberDomainResult
{
    public RemoveHouseholdMemberError? Error { get; }

    public bool IsSuccess => Error is null;

    private RemoveHouseholdMemberDomainResult(
        RemoveHouseholdMemberError? error)
    {
        Error = error;
    }

    public static RemoveHouseholdMemberDomainResult Success()
    {
        return new RemoveHouseholdMemberDomainResult(
            error: null);
    }

    public static RemoveHouseholdMemberDomainResult Failure(
        RemoveHouseholdMemberError error)
    {
        return new RemoveHouseholdMemberDomainResult(
            error);
    }
}
