namespace HomePlatform.Application.Accounts;

public interface IAccountAuthentication
{
    Task<AccountAuthenticationResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken);
}