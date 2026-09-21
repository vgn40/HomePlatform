using HomePlatform.Domain.Person;
using HomePlatform.Infrastructure.Persistence;
using HomePlatform.Application.Accounts.Register;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HomePlatform.Infrastructure.Identity;

public sealed class IdentityAccountRegistration(
    UserManager<ApplicationUser> userManager,
    HomePlatformDbContext context)
    : IAccountRegistration
{
    public async Task<AccountRegistrationResult> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedEmail = email.Trim();

        var person = new Person();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            UserName = normalizedEmail,
            Email = normalizedEmail
        };

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        context.Set<Person>().Add(person);
        var committed = false;
        try
        {
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return AccountRegistrationResult.Failure(
                    result.Errors
                        .Select(error => new AccountRegistrationError(MapErrorCode(error.Code)))
                        .Distinct()
                        .ToArray());
            }

            await transaction.CommitAsync(cancellationToken);
            committed = true;
            return AccountRegistrationResult.Success(user.Id);
        }
        catch (DbUpdateException exception) when (IsDuplicateAccount(exception))
        {
            return AccountRegistrationResult.Failure(
                new AccountRegistrationError(AccountRegistrationErrorCode.EmailAlreadyExists));
        }
        finally
        {
            if (!committed)
            {
                // Failed validation or persistence must not leak new entities into
                // a later SaveChanges in the same request scope.
                context.Entry(user).State = EntityState.Detached;
                context.Entry(person).State = EntityState.Detached;
            }
        }
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
