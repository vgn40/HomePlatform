namespace HomePlatform.Application.Accounts.Register;

public interface IAccountRegistration
{
    Task<AccountRegistrationResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken);
}
