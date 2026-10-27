using System.Net;
using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomePlatform.IntegrationTests.Households;

public sealed class CloseHouseholdEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlTestDatabase _postgres = new();

    private HomePlatformApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new HomePlatformApiFactory(
            _postgres.GetConnectionString());

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<HomePlatformDbContext>();

        await db.Database.MigrateAsync();

        _client = _factory.CreateClient();
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("missing", HttpStatusCode.NotFound)]
    [InlineData("nonmember", HttpStatusCode.NotFound)]
    [InlineData("member", HttpStatusCode.Forbidden)]
    [InlineData("guest", HttpStatusCode.Forbidden)]
    [InlineData("malformed", HttpStatusCode.NotFound)]
    [InlineData("invalid-auth", HttpStatusCode.Unauthorized)]
    public async Task Rejected_close_preserves_persisted_state(
        string scenario,
        HttpStatusCode expected)
    {
        var seed = await SeedAsync(
            scenario == "guest"
                ? HouseholdRole.Guest
                : HouseholdRole.Member);

        var before = await LoadAsync();

        Guid? accountId =
            scenario == "anonymous"
                ? null
                : seed.OwnerAccountId;

        if (scenario is "member" or "guest")
        {
            accountId = seed.TargetAccountId;
        }

        if (scenario == "nonmember")
        {
            accountId = Guid.NewGuid();

            await _factory.CreateIdentityAccountAsync(
                accountId.Value);
        }

        var householdId =
            scenario == "missing"
                ? Guid.NewGuid().ToString()
                : scenario == "malformed"
                    ? "not-a-guid"
                    : seed.HouseholdId.ToString();

        using var request =
            CreateRequest(
                accountId,
                householdId);

        if (scenario == "invalid-auth")
        {
            request.Headers.Remove(
                TestAuthenticationHandler.AccountIdHeaderName);

            request.Headers.Add(
                TestAuthenticationHandler.AccountIdHeaderName,
                "not-a-guid");
        }

        using var response =
            await _client.SendAsync(request);

        Assert.Equal(
            expected,
            response.StatusCode);

        HouseholdStateAssertions.AssertUnchanged(
            before,
            await LoadAsync());
    }

    [Fact]
    public async Task Owner_close_returns_empty_204_deletes_aggregate_and_preserves_accounts_and_other_household()
    {
        var seed = await SeedAsync();

        var before = await LoadAsync();

        var closing = Assert.Single(
            before,
            household =>
                household.Id == seed.HouseholdId);

        var membershipIds =
            closing.Members
                .Select(
                    member =>
                        member.MembershipId)
                .ToArray();

        using var request =
            CreateRequest(
                seed.OwnerAccountId,
                seed.HouseholdId.ToString());

        using var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        Assert.Empty(
            await response.Content
                .ReadAsByteArrayAsync());

        HouseholdStateAssertions.AssertUnchanged(
            before
                .Where(
                    household =>
                        household.Id != seed.HouseholdId)
                .ToList(),
            await LoadAsync());

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<HomePlatformDbContext>();

        Assert.False(
            await db.Set<Household>()
                .AnyAsync(
                    household =>
                        household.Id == seed.HouseholdId));

        Assert.False(
            await db.Set<HouseholdMember>()
                .AnyAsync(
                    member =>
                        member.HouseholdId ==
                        seed.HouseholdId));

        Assert.False(
            await db.Set<HouseholdMember>()
                .AnyAsync(
                    member =>
                        membershipIds.Contains(
                            member.MembershipId)));

        var accountPersonIds = await db.Users.Select(user => user.PersonId).ToArrayAsync();
        Assert.Equal(3, accountPersonIds.Length);
        foreach (var personId in accountPersonIds)
        {
            Assert.True(await db.Set<HomePlatform.Domain.Person.Person>()
                .AnyAsync(person => person.Id == personId));
        }
        var accountlessMember = Assert.Single(closing.Members,
            member => !accountPersonIds.Contains(member.PersonId));
        Assert.False(await db.Set<HomePlatform.Domain.Person.Person>()
            .AnyAsync(person => person.Id == accountlessMember.PersonId));
        foreach (var member in before.Where(household => household.Id != seed.HouseholdId)
            .SelectMany(household => household.Members))
        {
            Assert.True(await db.Set<HomePlatform.Domain.Person.Person>()
                .AnyAsync(person => person.Id == member.PersonId));
        }
    }

    [Fact]
    public async Task Route_is_not_exposed_in_production_and_preserves_state()
    {
        var seed = await SeedAsync();

        var before = await LoadAsync();

        await using var factory =
            new ProductionApiFactory(
                _postgres.GetConnectionString());

        using var client =
            factory.CreateClient();

        using var request =
            CreateRequest(
                seed.TargetAccountId,
                seed.HouseholdId.ToString());

        using var response =
            await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        HouseholdStateAssertions.AssertUnchanged(
            before,
            await LoadAsync());
    }

    private async Task<Seed> SeedAsync(
        HouseholdRole targetRole = HouseholdRole.Member)
    {
        var ownerAccountId = Guid.NewGuid();
        var targetAccountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();

        await _factory.CreateIdentityAccountAsync(
            ownerAccountId);

        await _factory.CreateIdentityAccountAsync(
            targetAccountId);

        await _factory.CreateIdentityAccountAsync(
            otherAccountId);

        var household =
            new Household(
                "Close contract",
                ownerAccountId);

        Assert.True(
            household.AddMember(
                household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
                targetRole,
                targetAccountId).IsSuccess);

        Assert.True(
            household.AddMember(
                household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
                HouseholdRole.Member,
                otherAccountId).IsSuccess);

        Assert.True(
            household.AddMember(
                household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
                HouseholdRole.Member, await _factory!.CreateLoginlessPersonAsync()).IsSuccess);

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<HomePlatformDbContext>();

        db.Add(household);

        var unrelated =
            new Household(
                "Unrelated home",
                otherAccountId);

        Assert.True(
            unrelated.AddMember(
                unrelated.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
                HouseholdRole.Guest, await _factory!.CreateLoginlessPersonAsync()).IsSuccess);

        db.Add(unrelated);

        await db.SaveChangesAsync();

        return new Seed(
            household.Id,
            ownerAccountId,
            targetAccountId);
    }

    private async Task<List<Household>> LoadAsync()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<HomePlatformDbContext>();

        return await db.Set<Household>()
            .AsNoTracking()
            .Include(
                household =>
                    household.Members)
            .OrderBy(
                household =>
                    household.Id)
            .ToListAsync();
    }

    private static HttpRequestMessage CreateRequest(
        Guid? accountId,
        string householdId)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Delete,
                $"/api/households/{householdId}");

        if (accountId is not null)
        {
            request.Headers.Add(
                TestAuthenticationHandler.AccountIdHeaderName,
                accountId.ToString());
        }

        return request;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    private sealed record Seed(
        Guid HouseholdId,
        Guid OwnerAccountId,
        Guid TargetAccountId);

    private sealed class ProductionApiFactory(
        string connectionString)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment(
                "Production");

            builder.UseSetting(
                "ConnectionStrings:Database",
                connectionString);
        }
    }
}
