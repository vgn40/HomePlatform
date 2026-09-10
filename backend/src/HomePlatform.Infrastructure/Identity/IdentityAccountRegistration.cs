using HomePlatform.Application.Accounts.Register;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HomePlatform.Infrastructure.Identity;

public sealed class IdentityAccountRegistration(
    UserManager<ApplicationUser> userManager)
    : IAccountRegistration
{
    public async Task<AccountRegistrationResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedEmail = email.Trim();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail,
            Email = normalizedEmail
        };

        IdentityResult result;

        try
        {
            result = await userManager.CreateAsync(
                user,
                password);
        }
        catch (DbUpdateException exception)
            when (IsDuplicateAccount(exception))
        {
            return AccountRegistrationResult.Failure(
                new AccountRegistrationError(
                    AccountRegistrationErrorCode.EmailAlreadyExists));
        }

        if (!result.Succeeded)
        {
            return AccountRegistrationResult.Failure(
                result.Errors
                    .Select(error =>
                        new AccountRegistrationError(
                            MapErrorCode(error.Code)))
                    .Distinct()
                    .ToArray());
        }

        return AccountRegistrationResult.Success(user.Id);
    }

    private static bool IsDuplicateAccount(
        DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "UserNameIndex"
        };
    }

    private static AccountRegistrationErrorCode MapErrorCode(
        string code) => code switch
    {
        nameof(IdentityErrorDescriber.DuplicateUserName)
            or nameof(IdentityErrorDescriber.DuplicateEmail)
            => AccountRegistrationErrorCode.EmailAlreadyExists,

        nameof(IdentityErrorDescriber.PasswordTooShort)
            => AccountRegistrationErrorCode.PasswordTooShort,

        nameof(IdentityErrorDescriber.PasswordRequiresDigit)
            => AccountRegistrationErrorCode.PasswordRequiresDigit,

        nameof(IdentityErrorDescriber.PasswordRequiresUpper)
            => AccountRegistrationErrorCode.PasswordRequiresUppercase,

        nameof(IdentityErrorDescriber.PasswordRequiresLower)
            => AccountRegistrationErrorCode.PasswordRequiresLowercase,

        nameof(IdentityErrorDescriber.PasswordRequiresNonAlphanumeric)
            => AccountRegistrationErrorCode.PasswordRequiresNonAlphanumeric,

        _ => AccountRegistrationErrorCode.RegistrationFailed
    };
}