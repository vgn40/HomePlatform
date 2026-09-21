using HomePlatform.Application.Identity;
using HomePlatform.Infrastructure.Identity;

namespace HomePlatform.IntegrationTests;

public sealed class IdentityCurrentPersonTests
{
    [Fact]
    public async Task Authenticated_account_resolves_person_and_forwards_cancellation_token()
    {
        var accountId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        var lookup = new RecordingLookup(personId);
        var currentPerson = new IdentityCurrentPerson(new CurrentAccount(accountId), lookup);

        Assert.Equal(personId, await currentPerson.GetPersonIdAsync(cancellation.Token));
        Assert.Equal(accountId, lookup.AccountId);
        Assert.Equal(cancellation.Token, lookup.CancellationToken);
        Assert.Equal(1, lookup.CallCount);
    }

    [Fact]
    public async Task Unauthenticated_account_returns_null_without_lookup()
    {
        var lookup = new RecordingLookup(Guid.NewGuid());
        var currentPerson = new IdentityCurrentPerson(new CurrentAccount(null), lookup);

        Assert.Null(await currentPerson.GetPersonIdAsync());
        Assert.Equal(0, lookup.CallCount);
    }

    [Fact]
    public async Task Unknown_or_deleted_account_returns_null()
    {
        var accountId = Guid.NewGuid();
        var lookup = new RecordingLookup(null);
        var currentPerson = new IdentityCurrentPerson(new CurrentAccount(accountId), lookup);

        Assert.Null(await currentPerson.GetPersonIdAsync());
        Assert.Equal(accountId, lookup.AccountId);
        Assert.Equal(1, lookup.CallCount);
    }

    [Fact]
    public async Task Lookup_cancellation_propagates()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var lookup = new RecordingLookup(Guid.NewGuid());
        var currentPerson = new IdentityCurrentPerson(new CurrentAccount(Guid.NewGuid()), lookup);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => currentPerson.GetPersonIdAsync(cancellation.Token));
        Assert.Equal(cancellation.Token, lookup.CancellationToken);
    }

    private sealed class CurrentAccount(Guid? accountId) : ICurrentAccount
    {
        public Guid AccountId => accountId ?? throw new UnauthorizedAccessException();
    }

    private sealed class RecordingLookup(Guid? personId) : IAccountPersonLookup
    {
        public Guid? AccountId { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public int CallCount { get; private set; }

        public Task<Guid?> GetPersonIdAsync(Guid accountId, CancellationToken cancellationToken = default)
        {
            AccountId = accountId;
            CancellationToken = cancellationToken;
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(personId);
        }

        public Task<bool> HasAccountForPersonAsync(Guid personId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Current Person resolution must not check ownership eligibility.");
    }
}
