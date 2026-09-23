namespace HomePlatform.Domain.Household;

public sealed record AddHouseholdMemberDomainResult
{
    public bool IsSuccess { get; }
    public AddHouseholdMemberError? Error { get; }

    private AddHouseholdMemberDomainResult(
        bool isSuccess,
        AddHouseholdMemberError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static AddHouseholdMemberDomainResult Success()
        => new(
            true,
            null);

    public static AddHouseholdMemberDomainResult Failure(
        AddHouseholdMemberError error)
        => new(
            false,
            error);
}