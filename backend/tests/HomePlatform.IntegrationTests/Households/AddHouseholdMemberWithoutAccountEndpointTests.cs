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
            HouseholdRole.Member);

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
    [InlineData(HouseholdRole.Owner)]
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
        Assert.Equal(HouseholdRole.Owner, Assert.Single(persisted.Members, m => m.PersonId == ownerId).Role);
        Assert.Equal(role == HouseholdRole.Owner ? 2 : 1, persisted.Members.Count(m => m.Role == HouseholdRole.Owner));
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

    [Theory]
    [InlineData("original")]
    [InlineData("added")]
    [InlineData("remaining")]
    public async Task Multiple_owner_lifecycle_preserves_other_owners_and_requires_explicit_close(string closingOwner)
    {
        var originalId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        foreach (var id in new[] { originalId, otherId, targetId })
            await _factory.CreateIdentityAccountAsync(id);
        var household = new Household("Home", originalId);
        Assert.True(household.AddMember(originalId, HouseholdRole.Owner, otherId).IsSuccess);
        Assert.True(household.AddMember(originalId, HouseholdRole.Member, targetId).IsSuccess);
        var target = household.Members.Single(m => m.PersonId == targetId);
        var originalMembership = household.Members.Single(m => m.PersonId == originalId).MembershipId;
        await SaveHouseholdAsync(household);
        var before = await ReloadAsync(household.Id);

        using (var invalid = await SendLifecycleAsync(HttpMethod.Put, "ownership", originalId,
            new { newOwnerMembershipId = household.Members.Single(m => m.PersonId == otherId).MembershipId }))
        {
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            var problem = await invalid.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
            Assert.Equal("New owner is already an owner of this household.", problem!.Detail);
        }
        var unchanged = await ReloadAsync(household.Id);
        Assert.Equal(before.UpdatedAt, unchanged.UpdatedAt);
        Assert.Equal(Roles(before), Roles(unchanged));

        // Either original or added Owner can close a household with multiple Owners.
        if (closingOwner != "remaining")
        {
            using var closed = await SendLifecycleAsync(HttpMethod.Delete, "", closingOwner == "original" ? originalId : otherId);
            Assert.Equal(HttpStatusCode.NoContent, closed.StatusCode);
        }
        else
        {
            using (var transfer = await SendLifecycleAsync(HttpMethod.Put, "ownership", originalId,
                new { newOwnerMembershipId = target.MembershipId }))
                Assert.Equal(HttpStatusCode.NoContent, transfer.StatusCode);
            var transferred = await ReloadAsync(household.Id);
            Assert.Equal(HouseholdRole.Member, transferred.Members.Single(m => m.PersonId == originalId).Role);
            Assert.Equal(HouseholdRole.Owner, transferred.Members.Single(m => m.PersonId == otherId).Role);
            Assert.Equal(HouseholdRole.Owner, transferred.Members.Single(m => m.PersonId == targetId).Role);
            Assert.Equal(originalMembership, transferred.Members.Single(m => m.PersonId == originalId).MembershipId);
            Assert.Equal(target.MembershipId, transferred.Members.Single(m => m.PersonId == targetId).MembershipId);

            using (var leave = await SendLifecycleAsync(HttpMethod.Delete, "membership", targetId))
                Assert.Equal(HttpStatusCode.NoContent, leave.StatusCode);
            var left = await ReloadAsync(household.Id);
            Assert.Equal(otherId, Assert.Single(left.Members, m => m.Role == HouseholdRole.Owner).PersonId);
            Assert.DoesNotContain(left.Members, m => m.PersonId == targetId);

            using (var rejected = await SendLifecycleAsync(HttpMethod.Delete, "membership", otherId))
                Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
            var rejectedState = await ReloadAsync(household.Id);
            Assert.Equal(left.UpdatedAt, rejectedState.UpdatedAt);
            Assert.Equal(Roles(left), Roles(rejectedState));

            using var closed = await SendLifecycleAsync(HttpMethod.Delete, "", otherId);
            Assert.Equal(HttpStatusCode.NoContent, closed.StatusCode);
        }
        await using var verification = _factory.Services.CreateAsyncScope();
        var db = verification.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        Assert.False(await db.Set<Household>().AnyAsync(h => h.Id == household.Id));
        Assert.False(await db.Set<HouseholdMember>().AnyAsync(m => EF.Property<Guid>(m, "HouseholdId") == household.Id));
        foreach (var id in new[] { originalId, otherId, targetId })
            Assert.True(await db.Set<ApplicationUser>().AnyAsync(u => u.PersonId == id));

        Task<HttpResponseMessage> SendLifecycleAsync(HttpMethod method, string suffix, Guid actorId, object? body = null)
        {
            var request = new HttpRequestMessage(method,
                $"/api/households/{household.Id}" + (suffix.Length == 0 ? "" : $"/{suffix}"));
            request.Headers.Add(TestAuthenticationHandler.AccountIdHeaderName, actorId.ToString());
            if (body is not null) request.Content = JsonContent.Create(body);
            return _client.SendAsync(request);
        }
    }

    [Fact]
    public async Task Sole_owner_leave_returns_conflict_and_preserves_household()
    {
        var ownerId = Guid.NewGuid();
        await _factory.CreateIdentityAccountAsync(ownerId);
        var household = new Household("Home", ownerId);
        await SaveHouseholdAsync(household);
        var before = await ReloadAsync(household.Id);
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/households/{household.Id}/membership");
        request.Headers.Add(TestAuthenticationHandler.AccountIdHeaderName, ownerId.ToString());

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var after = await ReloadAsync(household.Id);
        Assert.Equal(before.UpdatedAt, after.UpdatedAt);
        Assert.Equal(Roles(before), Roles(after));
    }

    private static (Guid, Guid, HouseholdRole)[] Roles(Household household)
        => household.Members.OrderBy(m => m.MembershipId).Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray();

    private async Task<Household> ReloadAsync(Guid id)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>()
            .Set<Household>().AsNoTracking().Include(h => h.Members).SingleAsync(h => h.Id == id);
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
