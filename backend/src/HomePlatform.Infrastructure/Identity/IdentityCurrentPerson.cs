using HomePlatform.Application.Identity;

namespace HomePlatform.Infrastructure.Identity;

public sealed class IdentityCurrentPerson(
    ICurrentAccount currentAccount,
    IAccountPersonLookup accountPersonLookup)
    : ICurrentPerson
{
    public Task<Guid?> GetPersonIdAsync(
        CancellationToken cancellationToken = default)
    {
        Guid accountId;

        try
        {
            accountId = currentAccount.AccountId;
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult<Guid?>(null);
        }

        return accountPersonLookup.GetPersonIdAsync(
            accountId,
            cancellationToken);
    }
}
