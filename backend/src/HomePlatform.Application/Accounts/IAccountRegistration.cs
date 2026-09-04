namespace HomePlatform.Application.Accounts;

public interface IAccountRegistration
{
    Task<AccountRegistrationResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken);
}

public sealed record AccountRegistrationResult(
    bool Succeeded,
    Guid? AccountId,
    IReadOnlyCollection<string> Errors);
}