using HomePlatform.Application.Accounts;
using Microsoft.AspNetCore.Identity;

namespace HomePlatform.Infrastructure.Identity;

public sealed class IdentityAccountAuthentication(
    UserManager<ApplicationUser> userManager)
    : IAccountAuthentication
{
    public async Task<AccountAuthenticationResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var trimmedEmail = email.Trim();

        var user = await userManager.FindByEmailAsync(trimmedEmail);

        if (user is null)
        {
            return AccountAuthenticationResult.Failure(
                new AccountAuthenticationError(
                    AccountAuthenticationErrorCode.InvalidCredentials));
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return AccountAuthenticationResult.Failure(
                new AccountAuthenticationError(
                    AccountAuthenticationErrorCode.AccountLocked));
        }

        var passwordIsValid =
            await userManager.CheckPasswordAsync(
                user,
                password);

        if (!passwordIsValid)
        {
            return AccountAuthenticationResult.Failure(
                new AccountAuthenticationError(
                    AccountAuthenticationErrorCode.InvalidCredentials));
        }

        return AccountAuthenticationResult.Success(user.Id);
    }
}