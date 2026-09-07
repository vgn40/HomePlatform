namespace HomePlatform.Application.Accounts;

public sealed record AccountRegistrationResult
{
    public bool Succeeded { get; }
    public Guid? AccountId { get; }
    public IReadOnlyCollection<AccountRegistrationError> Errors { get; }

    private AccountRegistrationResult(
        bool succeeded,
        Guid? accountId,
        IReadOnlyCollection<AccountRegistrationError> errors)
    {
        Succeeded = succeeded;
        AccountId = accountId;
        Errors = errors;
    }

    public static AccountRegistrationResult Success(Guid accountId)
    {
        return new AccountRegistrationResult(
            true,
            accountId,
            []);
    }

    public static AccountRegistrationResult Failure(
        params AccountRegistrationError[] errors)
    {
        return new AccountRegistrationResult(
            false,
            null,
            errors);
    }
}
