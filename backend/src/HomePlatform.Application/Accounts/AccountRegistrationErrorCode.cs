namespace HomePlatform.Application.Accounts;

public enum AccountRegistrationErrorCode
{
    EmailRequired,
    EmailInvalid,
    EmailTooLong,

    PasswordRequired,
    PasswordTooShort,
    PasswordRequiresDigit,
    PasswordRequiresUppercase,
    PasswordRequiresLowercase,
    PasswordRequiresNonAlphanumeric,

    EmailAlreadyExists,

    RegistrationFailed
}
