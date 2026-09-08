namespace HomePlatform.Application.Accounts;

public enum AccountAuthenticationErrorCode
{
    EmailRequired,
    PasswordRequired,

    InvalidCredentials,
    AccountLocked,

    AuthenticationFailed
}