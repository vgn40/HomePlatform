using HomePlatform.Application.Identity;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomePlatform.Infrastructure.Identity;

public sealed class IdentityAccountPersonLookup(
    HomePlatformDbContext context)
    : IAccountPersonLookup
{
    public Task<Guid?> GetPersonIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => context.Users
            .Where(user => user.Id == accountId)
            .Select(user => (Guid?)user.PersonId)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> HasAccountForPersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
        => context.Users
            .AnyAsync(
                user => user.PersonId == personId,
                cancellationToken);
}
