using System.Security.Claims;
using HomePlatform.Application.Identity;

namespace HomePlatform.Api.Identity;

public sealed class HttpCurrentAccount(
    IHttpContextAccessor httpContextAccessor)
    : ICurrentAccount
{
    public Guid AccountId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException(
                    "Authenticated account is required.");
            }

            var subject =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value;

            if (!Guid.TryParse(subject, out var accountId) ||
                accountId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "Authenticated account id is invalid.");
            }

            return accountId;
        }
    }
}
