namespace HomePlatform.Application.Accounts.SignIn;

public enum AccountAuthenticationErrorCode
{
    EmailRequired,
    PasswordRequired,

    InvalidCredentials,
    AccountLocked,

    AuthenticationFailed
}