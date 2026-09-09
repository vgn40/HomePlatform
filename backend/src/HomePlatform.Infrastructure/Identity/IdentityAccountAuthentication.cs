using HomePlatform.Application.Accounts;
using Microsoft.AspNetCore.Identity;

namespace HomePlatform.Infrastructure.Identity;

public sealed class IdentityAccountAuthentication(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager)
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

        signInManager.AuthenticationScheme =
            IdentityConstants.BearerScheme;

        var result = await signInManager.PasswordSignInAsync(
            user,
            password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return AccountAuthenticationResult.Failure(
                new AccountAuthenticationError(
                    AccountAuthenticationErrorCode.AccountLocked));
        }

        if (!result.Succeeded)
        {
            return AccountAuthenticationResult.Failure(
                new AccountAuthenticationError(
                    AccountAuthenticationErrorCode.InvalidCredentials));
        }

        return AccountAuthenticationResult.Success(user.Id);
    }
}