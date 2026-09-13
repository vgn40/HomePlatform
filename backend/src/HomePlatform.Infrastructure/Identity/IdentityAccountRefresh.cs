using HomePlatform.Application.Accounts.Refresh;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HomePlatform.Infrastructure.Identity;

public sealed class IdentityAccountRefresh(
    SignInManager<ApplicationUser> signInManager,
    IOptionsMonitor<BearerTokenOptions> bearerTokenOptions,
    TimeProvider timeProvider)
    : IAccountRefresh
{
    public async Task<AccountRefreshResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var refreshTokenProtector = bearerTokenOptions
            .Get(IdentityConstants.BearerScheme)
            .RefreshTokenProtector;

        var refreshTicket =
            refreshTokenProtector.Unprotect(refreshToken);

        if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc
            || timeProvider.GetUtcNow() >= expiresUtc
            || await signInManager.ValidateSecurityStampAsync(
                refreshTicket.Principal) is not ApplicationUser user)
        {
            return AccountRefreshResult.Failure(
                new AccountRefreshError(
                    AccountRefreshErrorCode.InvalidRefreshToken));
        }

        var newPrincipal =
            await signInManager.CreateUserPrincipalAsync(user);

        await signInManager.Context.SignInAsync(
            IdentityConstants.BearerScheme,
            newPrincipal);

        return AccountRefreshResult.Success();
    }
}