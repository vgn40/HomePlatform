using System.Net;
using System.Net.Http.Json;
using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests.Households;

public sealed class LeaveHouseholdEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine")
            .WithDatabase("homeplatform")
            .WithUsername("homeplatform")
            .WithPassword("homeplatform-dev")
            .Build();

    private HomePlatformApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new HomePlatformApiFactory(_postgres.GetConnectionString());
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        await db.Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("missing", HttpStatusCode.NotFound)]
    [InlineData("nonmember", HttpStatusCode.Forbidden)]
    [InlineData("owner", HttpStatusCode.Conflict)]
    [InlineData("malformed", HttpStatusCode.NotFound)]
    [InlineData("invalid-auth", HttpStatusCode.Unauthorized)]
    public async Task Rejected_leave_preserves_persisted_state(string scenario, HttpStatusCode expected)
    {
        var seed = await SeedAsync();
        var before = await LoadAsync();
        Guid? accountId = scenario == "anonymous" ? null : seed.OwnerAccountId;
        if (scenario == "nonmember")
        {
            accountId = Guid.NewGuid();
            await _factory.CreateIdentityAccountAsync(accountId.Value);
        }
        var householdId = scenario == "missing" ? Guid.NewGuid().ToString()
            : scenario == "malformed" ? "not-a-guid" : seed.HouseholdId.ToString();
        using var request = CreateRequest(accountId, householdId);
        if (scenario == "invalid-auth")
        {
            request.Headers.Remove(TestAuthenticationHandler.AccountIdHeaderName);
            request.Headers.Add(TestAuthenticationHandler.AccountIdHeaderName, "not-a-guid");
        }
        using var response = await _client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
        if (scenario == "owner")
        {
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal(409, problem.Status);
            Assert.Equal("Cannot leave household", problem.Title);
            Assert.Equal("Owner must transfer ownership or close the household before leaving.", problem.Detail);
        }
        AssertUnchanged(before, await LoadAsync());
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Successful_leave_returns_empty_204_deletes_exact_member_and_preserves_household(HouseholdRole role)
    {
        var seed = await SeedAsync(role);
        var before = Assert.Single(await LoadAsync());
        using var request = CreateRequest(seed.TargetAccountId, seed.HouseholdId.ToString());
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        var after = Assert.Single(await LoadAsync());
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(before.Name, after.Name);
        Assert.Equal(before.CreatedAt, after.CreatedAt);
        Assert.Equal(before.Members.Count - 1, after.Members.Count);
        Assert.Equal(
            before.Members.Where(member => member.MembershipId != seed.TargetId).OrderBy(member => member.MembershipId)
                .Select(member => (member.MembershipId, member.AccountId, member.Role)).ToArray(),
            after.Members.OrderBy(member => member.MembershipId)
                .Select(member => (member.MembershipId, member.AccountId, member.Role)).ToArray());
        Assert.Equal(seed.OwnerId, Assert.Single(after.Members, member => member.Role == HouseholdRole.Owner).MembershipId);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        Assert.False(await db.Set<HouseholdMember>().AnyAsync(member => member.MembershipId == seed.TargetId));
    }

    [Fact]
    public async Task Route_is_not_exposed_in_production_and_preserves_state()
    {
        var seed = await SeedAsync();
        var before = await LoadAsync();
        await using var factory = new ProductionApiFactory(_postgres.GetConnectionString());
        using var client = factory.CreateClient();
        using var request = CreateRequest(seed.TargetAccountId, seed.HouseholdId.ToString());
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertUnchanged(before, await LoadAsync());
    }

    private async Task<Seed> SeedAsync(
        HouseholdRole targetRole = HouseholdRole.Member,
        HouseholdRole callerRole = HouseholdRole.Member)
    {
        var ownerAccountId = Guid.NewGuid();
        var targetAccountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        await _factory.CreateIdentityAccountAsync(ownerAccountId);
        await _factory.CreateIdentityAccountAsync(targetAccountId);
        await _factory.CreateIdentityAccountAsync(otherAccountId);
        var household = new Household("Ownership contract", ownerAccountId);
        Assert.True(household.AddMember(targetRole, targetAccountId).IsSuccess);
        Assert.True(household.AddMember(callerRole, otherAccountId).IsSuccess);
        Assert.True(household.AddMember(HouseholdRole.Member).IsSuccess);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        db.Add(household);
        await db.SaveChangesAsync();
        return new Seed(household.Id, ownerAccountId, targetAccountId,
            household.Members.Single(member => member.AccountId == ownerAccountId).MembershipId,
            household.Members.Single(member => member.AccountId == targetAccountId).MembershipId,
            household.Members.Single(member => member.AccountId is null).MembershipId);
    }

    private async Task<List<Household>> LoadAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        return await db.Set<Household>().AsNoTracking()
            .Include(household => household.Members).OrderBy(household => household.Id).ToListAsync();
    }

    private static void AssertUnchanged(List<Household> before, List<Household> after)
    {
        Assert.Equal(before.Count, after.Count);
        foreach (var original in before)
        {
            var current = Assert.Single(after, household => household.Id == original.Id);
            Assert.Equal(original.Name, current.Name);
            Assert.Equal(original.CreatedAt, current.CreatedAt);
            Assert.Equal(original.UpdatedAt, current.UpdatedAt);
            Assert.Equal(
                original.Members.OrderBy(member => member.MembershipId)
                    .Select(member => (member.MembershipId, member.AccountId, member.Role)).ToArray(),
                current.Members.OrderBy(member => member.MembershipId)
                    .Select(member => (member.MembershipId, member.AccountId, member.Role)).ToArray());
        }
    }

    private static HttpRequestMessage CreateRequest(Guid? accountId, string householdId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/households/{householdId}/membership");
        if (accountId is not null)
            request.Headers.Add(TestAuthenticationHandler.AccountIdHeaderName, accountId.ToString());
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

    private sealed record Seed(Guid HouseholdId, Guid OwnerAccountId, Guid TargetAccountId,
        Guid OwnerId, Guid TargetId, Guid LoginlessId);

    private sealed class ProductionApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:Database", connectionString);
        }
    }
}
