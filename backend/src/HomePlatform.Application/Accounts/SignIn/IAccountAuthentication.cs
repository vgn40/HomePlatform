namespace HomePlatform.Application.Accounts.SignIn;

public interface IAccountAuthentication
{
    Task<AccountAuthenticationResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken);
}