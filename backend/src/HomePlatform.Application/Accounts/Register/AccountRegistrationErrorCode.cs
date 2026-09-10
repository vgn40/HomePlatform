namespace HomePlatform.Application.Accounts.Register;

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
