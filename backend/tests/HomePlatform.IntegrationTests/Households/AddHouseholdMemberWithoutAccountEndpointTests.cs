using System.Net;
using System.Net.Http.Json;
using HomePlatform.Api.Households;
using HomePlatform.Application.Households.AddHouseholdMemberWithoutAccount;
using HomePlatform.Domain.Household;
using HomePlatform.Domain.Person;
using HomePlatform.Infrastructure.Identity;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests.Households;

public sealed class AddHouseholdMemberWithoutAccountEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6-alpine")
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
        await scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>().Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("missing", HttpStatusCode.NotFound)]
    [InlineData("outsider", HttpStatusCode.NotFound)]
    [InlineData("member", HttpStatusCode.Forbidden)]
    [InlineData("guest", HttpStatusCode.Forbidden)]
    [InlineData("owner-role", HttpStatusCode.BadRequest)]
    [InlineData("empty", HttpStatusCode.BadRequest)]
    [InlineData("blank", HttpStatusCode.BadRequest)]
    public async Task Add_member_failure_preserves_database(string scenario, HttpStatusCode expected)
    {
        var ownerId = Guid.NewGuid();
        await _factory.CreateIdentityAccountAsync(ownerId);
        var household = new Household("Home", ownerId);
        var actorId = ownerId;
        if (scenario is "outsider" or "member" or "guest")
        {
            actorId = Guid.NewGuid();
            await _factory.CreateIdentityAccountAsync(actorId);
            if (scenario != "outsider")
                Assert.True(household.AddMember(ownerId,
                    scenario == "member" ? HouseholdRole.Member : HouseholdRole.Guest, actorId).IsSuccess);
        }
        await SaveHouseholdAsync(household);
        var before = household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).OrderBy(m => m.MembershipId).ToArray();
        int personCount;
        int accountCount;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
            personCount = await db.Set<Person>().CountAsync();
            accountCount = await db.Set<ApplicationUser>().CountAsync();
        }
        using var request = CreateRequest(scenario == "missing" ? Guid.NewGuid() : household.Id,
            scenario == "anonymous" ? null : actorId,
            scenario == "empty" ? "" : scenario == "blank" ? "   " : "Alma",
            scenario == "owner-role" ? HouseholdRole.Owner : HouseholdRole.Member);

        using var response = await _client.SendAsync(request);

        Assert.Equal(expected, response.StatusCode);
        await using var verification = _factory.Services.CreateAsyncScope();
        var context = verification.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        Assert.Equal(personCount, await context.Set<Person>().CountAsync());
        Assert.Equal(accountCount, await context.Set<ApplicationUser>().CountAsync());
        var persisted = await context.Set<Household>().Include(h => h.Members).SingleAsync(h => h.Id == household.Id);
        Assert.Equal(before, persisted.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).OrderBy(m => m.MembershipId).ToArray());
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Add_member_returns_201_and_commits_accountless_person_and_membership(HouseholdRole role)
    {
        var ownerId = Guid.NewGuid();
        await _factory.CreateIdentityAccountAsync(ownerId);
        var household = new Household("Home", ownerId);
        await SaveHouseholdAsync(household);
        using var request = CreateRequest(household.Id, ownerId, "  Alma  ", role);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AddHouseholdMemberWithoutAccountResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.PersonId);
        Assert.NotEqual(Guid.Empty, body.MembershipId);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        var person = await db.Set<Person>().SingleAsync(p => p.Id == body.PersonId);
        Assert.Equal("Alma", person.DisplayName);
        Assert.False(await db.Set<ApplicationUser>().AnyAsync(user => user.PersonId == person.Id));
        var persisted = await db.Set<Household>().Include(h => h.Members).SingleAsync(h => h.Id == household.Id);
        Assert.Equal(2, persisted.Members.Count);
        var member = Assert.Single(persisted.Members, m => m.PersonId == person.Id);
        Assert.Equal(body.MembershipId, member.MembershipId);
        Assert.Equal(role, member.Role);
        Assert.Equal(ownerId, Assert.Single(persisted.Members, m => m.Role == HouseholdRole.Owner).PersonId);
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AddHouseholdMemberWithoutAccountHandler>());
        Assert.IsType<AddHouseholdMemberWithoutAccountPersistence>(
            scope.ServiceProvider.GetRequiredService<IAddHouseholdMemberWithoutAccountPersistence>());
    }

    [Fact]
    public async Task Persistence_foreign_key_failure_rolls_back_person_and_membership_together()
    {
        var ownerId = Guid.NewGuid();
        await _factory.CreateIdentityAccountAsync(ownerId);
        var household = new Household("Home", ownerId);
        await SaveHouseholdAsync(household);
        var person = new Person("Alma");
        var missingPersonId = Guid.NewGuid();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
            var loaded = await db.Set<Household>().Include(h => h.Members).SingleAsync(h => h.Id == household.Id);
            Assert.True(loaded.AddMember(ownerId, HouseholdRole.Member, missingPersonId).IsSuccess);
            var persistence = scope.ServiceProvider.GetRequiredService<IAddHouseholdMemberWithoutAccountPersistence>();

            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => persistence.SaveAsync(person, loaded));

            var postgres = Assert.IsType<PostgresException>(exception.InnerException);
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgres.SqlState);
        }
        await using var verification = _factory.Services.CreateAsyncScope();
        var context = verification.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        Assert.False(await context.Set<Person>().AnyAsync(p => p.Id == person.Id));
        Assert.False(await context.Set<HouseholdMember>().AnyAsync(m => m.PersonId == missingPersonId));
        var persisted = await context.Set<Household>().Include(h => h.Members).SingleAsync(h => h.Id == household.Id);
        Assert.Equal(ownerId, Assert.Single(persisted.Members).PersonId);
    }

    private async Task SaveHouseholdAsync(Household household)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        db.Set<Household>().Add(household);
        await db.SaveChangesAsync();
    }

    private static HttpRequestMessage CreateRequest(Guid householdId, Guid? accountId, string displayName, HouseholdRole role)
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"/api/households/{householdId}/members/without-account")
        {
            Content = JsonContent.Create(new { displayName, role = role.ToString() })
        };
        if (accountId is not null)
            request.Headers.Add(TestAuthenticationHandler.AccountIdHeaderName, accountId.ToString());
        return request;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
