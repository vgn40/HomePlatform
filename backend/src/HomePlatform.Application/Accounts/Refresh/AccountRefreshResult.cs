namespace HomePlatform.Application.Accounts.Refresh;

public sealed record AccountRefreshResult
{
    public bool Succeeded { get; }

    public IReadOnlyCollection<AccountRefreshError> Errors { get; }

    private AccountRefreshResult(
        bool succeeded,
        IReadOnlyCollection<AccountRefreshError> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public static AccountRefreshResult Success()
    {
        return new AccountRefreshResult(
            true,
            []);
    }

    public static AccountRefreshResult Failure(
        params AccountRefreshError[] errors)
    {
        return new AccountRefreshResult(
            false,
            errors);
    }
}
