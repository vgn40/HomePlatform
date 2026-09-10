namespace HomePlatform.Application.Accounts.SignIn;

public sealed record AccountAuthenticationResult
{
    public bool Succeeded { get; }
    public Guid? AccountId { get; }
    public IReadOnlyCollection<AccountAuthenticationError> Errors { get; }

    private AccountAuthenticationResult(
        bool succeeded,
        Guid? accountId,
        IReadOnlyCollection<AccountAuthenticationError> errors)
    {
        Succeeded = succeeded;
        AccountId = accountId;
        Errors = errors;
    }

    public static AccountAuthenticationResult Success(Guid accountId)
    {
        return new AccountAuthenticationResult(
            true,
            accountId,
            []);
    }

    public static AccountAuthenticationResult Failure(
        params AccountAuthenticationError[] errors)
    {
        return new AccountAuthenticationResult(
            false,
            null,
            errors);
    }
}