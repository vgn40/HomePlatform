using System.Data.Common;
using System.Net;
using System.Text.Json;
using HomePlatform.Domain.Household;
using HomePlatform.Domain.Person;
using HomePlatform.Infrastructure.Identity;
using HomePlatform.Infrastructure.Persistence;
using HomePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HomePlatform.IntegrationTests.Households;

public sealed class RemoveHouseholdMemberTests : IAsyncLifetime
{
    private readonly PostgreSqlTestDatabase _postgres = new();
    private readonly CleanupGate _gate = new();
    private HomePlatformApiFactory _factory = null!;
    private HttpClient _client = null!;
    private readonly Guid _ownerAccount = Guid.NewGuid();
    private Guid _ownerPerson;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new HomePlatformApiFactory(_postgres.GetConnectionString(), services =>
            services.AddDbContext<HomePlatformDbContext>(options => options.AddInterceptors(_gate)));
        await using var db = Context();
        await db.Database.MigrateAsync();
        var owner = new Person("Owner");
        _ownerPerson = owner.Id;
        db.Add(owner);
        db.Users.Add(new ApplicationUser { Id = _ownerAccount, PersonId = owner.Id });
        await db.SaveChangesAsync();
        _client = _factory.CreateClient();
    }

    [Theory]
    [InlineData(HouseholdRole.Member, "exclusive-named")]
    [InlineData(HouseholdRole.Guest, "exclusive-named")]
    [InlineData(HouseholdRole.Member, "exclusive-unnamed")]
    [InlineData(HouseholdRole.Guest, "exclusive-unnamed")]
    [InlineData(HouseholdRole.Member, "account")]
    [InlineData(HouseholdRole.Guest, "account")]
    [InlineData(HouseholdRole.Member, "shared")]
    [InlineData(HouseholdRole.Guest, "shared")]
    public async Task Delete_persists_membership_removal_and_safe_person_cleanup(HouseholdRole role, string policy)
    {
        var seed = await SeedAsync(role, policy);
        var otherBefore = await HouseholdStateAsync(seed.Other.Id);
        var accountsBefore = await AccountStateAsync();

        using var response = await SendAsync(seed.Home.Id, seed.MembershipId);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        await using var db = Context();
        Assert.False(await db.Set<HouseholdMember>().AnyAsync(m => m.MembershipId == seed.MembershipId));
        Assert.Equal(policy is "account" or "shared", await db.Set<Person>().AnyAsync(p => p.Id == seed.Target.Id));
        var home = await db.Set<Household>().Include(h => h.Members).SingleAsync(h => h.Id == seed.Home.Id);
        Assert.Equal(_ownerPerson, Assert.Single(home.Members).PersonId);
        Assert.Equal(seed.Home.OwnershipVersion, home.OwnershipVersion);
        Assert.Equal(otherBefore, await HouseholdStateAsync(seed.Other.Id));
        Assert.Equal(accountsBefore, await AccountStateAsync());
        Assert.True(await db.Set<Person>().AnyAsync(p => p.Id == seed.UnrelatedPerson.Id));
        Assert.True(await db.Set<Person>().AnyAsync(p => p.Id == _ownerPerson));
        await AssertNoBrokenReferencesAsync();
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("unknown-account", HttpStatusCode.Unauthorized)]
    [InlineData("missing-home", HttpStatusCode.NotFound)]
    [InlineData("outsider", HttpStatusCode.NotFound)]
    [InlineData("member", HttpStatusCode.Forbidden)]
    [InlineData("guest", HttpStatusCode.Forbidden)]
    [InlineData("missing-target", HttpStatusCode.NotFound)]
    [InlineData("other-household-target", HttpStatusCode.NotFound)]
    [InlineData("owner", HttpStatusCode.Conflict)]
    [InlineData("self", HttpStatusCode.Conflict)]
    public async Task Delete_errors_preserve_full_database_state(string scenario, HttpStatusCode expected)
    {
        var seed = await SeedAsync(scenario == "guest" ? HouseholdRole.Guest : HouseholdRole.Member, "account");
        var actor = _ownerAccount;
        var household = seed.Home.Id;
        var membership = seed.MembershipId;
        if (scenario == "unknown-account") actor = Guid.NewGuid();
        if (scenario is "member" or "guest")
        {
            await using var db = Context();
            actor = await db.Users.Where(u => u.PersonId == seed.Target.Id).Select(u => u.Id).SingleAsync();
        }
        if (scenario == "outsider")
        {
            actor = Guid.NewGuid();
            await _factory.CreateIdentityAccountAsync(actor);
        }
        if (scenario == "missing-home") household = Guid.NewGuid();
        if (scenario == "missing-target") membership = Guid.NewGuid();
        if (scenario == "other-household-target") membership = seed.Other.Members.Single(m => m.PersonId == seed.UnrelatedPerson.Id).MembershipId;
        if (scenario == "self") membership = seed.Home.Members.Single(m => m.PersonId == _ownerPerson).MembershipId;
        if (scenario == "owner")
        {
            await using var db = Context();
            var home = await db.Set<Household>().Include(h => h.Members).SingleAsync(h => h.Id == household);
            Assert.True(home.AddMember(_ownerPerson, HouseholdRole.Owner, seed.UnrelatedPerson.Id).IsSuccess);
            membership = home.Members.Single(m => m.PersonId == seed.UnrelatedPerson.Id).MembershipId;
            await db.SaveChangesAsync();
        }
        var before = await StateAsync();

        using var response = await SendAsync(household, membership, scenario == "anonymous" ? null : actor);

        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(before, await StateAsync());
    }

    [Theory]
    [InlineData("exclusive-named")]
    [InlineData("exclusive-unnamed")]
    [InlineData("account")]
    [InlineData("shared")]
    public async Task Close_cleans_only_eligible_people_including_accountless_owners(string policy)
    {
        var seed = await SeedAsync(HouseholdRole.Guest, policy);
        var accountlessOwner = new Person("Co-owner");
        await using (var db = Context())
        {
            db.Add(accountlessOwner);
            var home = await db.Set<Household>().Include(h => h.Members).SingleAsync(h => h.Id == seed.Home.Id);
            Assert.True(home.AddMember(_ownerPerson, HouseholdRole.Owner, accountlessOwner.Id).IsSuccess);
            await db.SaveChangesAsync();
        }
        var otherBefore = await HouseholdStateAsync(seed.Other.Id);
        var accountsBefore = await AccountStateAsync();

        using var response = await SendAsync(seed.Home.Id, null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using var verification = Context();
        Assert.False(await verification.Set<Household>().AnyAsync(h => h.Id == seed.Home.Id));
        Assert.False(await verification.Set<HouseholdMember>().AnyAsync(m => m.HouseholdId == seed.Home.Id));
        Assert.False(await verification.Set<Person>().AnyAsync(p => p.Id == accountlessOwner.Id));
        Assert.Equal(policy is "account" or "shared", await verification.Set<Person>().AnyAsync(p => p.Id == seed.Target.Id));
        Assert.True(await verification.Set<Person>().AnyAsync(p => p.Id == _ownerPerson));
        Assert.True(await verification.Set<Person>().AnyAsync(p => p.Id == seed.UnrelatedPerson.Id));
        Assert.Equal(otherBefore, await HouseholdStateAsync(seed.Other.Id));
        Assert.Equal(accountsBefore, await AccountStateAsync());
        await AssertNoBrokenReferencesAsync();
    }

    [Fact]
    public async Task Unexpected_person_fk_failure_during_membership_save_remains_500()
    {
        var seed = await SeedAsync();
        await using (var db = Context())
            await db.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION invalid_person_reference() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    INSERT INTO "HouseholdMember" ("MembershipId", "PersonId", "Role", "HouseholdId")
                    VALUES (gen_random_uuid(), gen_random_uuid(), 1, NEW."Id");
                    RETURN NEW;
                END; $$;
                CREATE TRIGGER invalid_person_reference BEFORE UPDATE ON "Household"
                FOR EACH ROW EXECUTE FUNCTION invalid_person_reference();
                """);
        var before = await StateAsync();
        using var response = await SendAsync(seed.Home.Id, seed.MembershipId);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("FK_HouseholdMember_Person_PersonId", await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await StateAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cleanup_failure_rolls_back_entire_operation_and_remains_500(bool close)
    {
        var seed = await SeedAsync();
        await using (var db = Context())
            await db.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION reject_person_cleanup() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Unexpected cleanup failure'; END; $$;
                CREATE TRIGGER reject_person_cleanup BEFORE DELETE ON "Person"
                FOR EACH ROW EXECUTE FUNCTION reject_person_cleanup();
                """);
        var before = await StateAsync();

        using var response = await SendAsync(seed.Home.Id, close ? null : seed.MembershipId);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("Unexpected cleanup failure", await response.Content.ReadAsStringAsync());
        Assert.Equal(before, await StateAsync());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Reference_committed_during_cleanup_is_preserved_and_conflict_rolls_back(bool close, bool account)
    {
        var seed = await SeedAsync();
        _gate.Arm(1);
        var pending = SendAsync(seed.Home.Id, close ? null : seed.MembershipId);
        string winner;
        try
        {
            await _gate.Reached.Task.WaitAsync(TimeSpan.FromSeconds(20));
            // Deliberately use the normal Read Committed writer. Person FKs must
            // protect references even when the other writer is not serializable.
            await using var db = Context();
            if (account)
                db.Users.Add(new ApplicationUser { Id = Guid.NewGuid(), PersonId = seed.Target.Id });
            else
            {
                var other = await db.Set<Household>().Include(h => h.Members).SingleAsync(h => h.Id == seed.Other.Id);
                Assert.True(other.AddMember(_ownerPerson, HouseholdRole.Member, seed.Target.Id).IsSuccess);
            }
            await db.SaveChangesAsync();
            winner = await StateAsync();
        }
        finally { _gate.Release.TrySetResult(); }

        using var response = await pending.WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(winner, await StateAsync());
        // A fresh retry succeeds and now sees the dependency, preserving Person.
        using var retry = await SendAsync(seed.Home.Id, close ? null : seed.MembershipId);
        Assert.Equal(HttpStatusCode.NoContent, retry.StatusCode);
        await using var verification = Context();
        Assert.True(await verification.Set<Person>().AnyAsync(p => p.Id == seed.Target.Id));
        await AssertNoBrokenReferencesAsync();
    }

    [Fact]
    public async Task Concurrent_last_membership_removals_conflict_then_retry_deletes_orphan()
    {
        var seed = await SeedAsync(HouseholdRole.Member, "shared");
        var otherMembership = seed.Other.Members.Single(m => m.PersonId == seed.Target.Id).MembershipId;
        _gate.Arm(2);
        var first = SendAsync(seed.Home.Id, seed.MembershipId);
        var second = SendAsync(seed.Other.Id, otherMembership);
        try { await _gate.Reached.Task.WaitAsync(TimeSpan.FromSeconds(20)); }
        finally { _gate.Release.TrySetResult(); }
        using var a = await first.WaitAsync(TimeSpan.FromSeconds(20));
        using var b = await second.WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Equal(new[] { HttpStatusCode.NoContent, HttpStatusCode.Conflict },
            new[] { a.StatusCode, b.StatusCode }.OrderBy(s => s).ToArray());
        await using (var db = Context())
        {
            Assert.True(await db.Set<Person>().AnyAsync(p => p.Id == seed.Target.Id));
            Assert.Single(await db.Set<HouseholdMember>().Where(m => m.PersonId == seed.Target.Id).ToArrayAsync());
        }
        using var retry = a.StatusCode == HttpStatusCode.Conflict
            ? await SendAsync(seed.Home.Id, seed.MembershipId)
            : await SendAsync(seed.Other.Id, otherMembership);
        Assert.Equal(HttpStatusCode.NoContent, retry.StatusCode);
        await using var verification = Context();
        Assert.False(await verification.Set<Person>().AnyAsync(p => p.Id == seed.Target.Id));
        Assert.False(await verification.Set<HouseholdMember>().AnyAsync(m => m.PersonId == seed.Target.Id));
        await AssertNoBrokenReferencesAsync();
    }

    [Fact]
    public async Task Closing_stale_aggregate_also_cleans_person_added_after_load()
    {
        var seed = await SeedAsync();
        await using var closing = Context();
        var repository = new HouseholdRepository(closing);
        var loaded = (await repository.GetByIdAsync(seed.Home.Id))!;
        var later = new Person("Later member");
        await using (var adding = Context())
        {
            adding.Add(later);
            var home = await adding.Set<Household>().Include(h => h.Members).SingleAsync(h => h.Id == seed.Home.Id);
            Assert.True(home.AddMember(_ownerPerson, HouseholdRole.Guest, later.Id).IsSuccess);
            await new AddHouseholdMemberWithoutAccountPersistence(adding).SaveAsync(later, home);
        }
        Assert.True(loaded.Close(_ownerPerson).IsSuccess);
        await repository.DeleteAsync(loaded);
        await using var verification = Context();
        Assert.False(await verification.Set<Person>().AnyAsync(p => p.Id == later.Id || p.Id == seed.Target.Id));
        Assert.False(await verification.Set<HouseholdMember>().AnyAsync(m => m.HouseholdId == seed.Home.Id));
    }

    private async Task<Seed> SeedAsync(HouseholdRole role = HouseholdRole.Member, string policy = "exclusive-named")
    {
        var target = new Person(policy == "exclusive-unnamed" ? null : "Target");
        var unrelated = new Person("Unrelated");
        var home = new Household("Home", _ownerPerson);
        var other = new Household("Other", _ownerPerson);
        Assert.True(home.AddMember(_ownerPerson, role, target.Id).IsSuccess);
        Assert.True(other.AddMember(_ownerPerson, HouseholdRole.Guest, unrelated.Id).IsSuccess);
        if (policy == "shared") Assert.True(other.AddMember(_ownerPerson, HouseholdRole.Member, target.Id).IsSuccess);
        await using var db = Context();
        db.AddRange(target, unrelated, home, other);
        if (policy == "account") db.Users.Add(new ApplicationUser { Id = Guid.NewGuid(), PersonId = target.Id });
        await db.SaveChangesAsync();
        return new Seed(home, other, target, unrelated, home.Members.Single(m => m.PersonId == target.Id).MembershipId);
    }

    private Task<HttpResponseMessage> SendAsync(Guid household, Guid? membership)
        => SendAsync(household, membership, _ownerAccount);

    private async Task<HttpResponseMessage> SendAsync(Guid household, Guid? membership, Guid? actor)
    {
        var path = $"/api/households/{household}" + (membership is null ? "" : $"/members/{membership}");
        using var request = new HttpRequestMessage(HttpMethod.Delete, path);
        if (actor is not null) request.Headers.Add(TestAuthenticationHandler.AccountIdHeaderName, actor.ToString());
        return await _client.SendAsync(request);
    }

    private HomePlatformDbContext Context() => new(new DbContextOptionsBuilder<HomePlatformDbContext>()
        .UseNpgsql(_postgres.GetConnectionString()).Options);

    private async Task<string> HouseholdStateAsync(Guid id)
    {
        await using var db = Context();
        var h = await db.Set<Household>().AsNoTracking().Include(h => h.Members).SingleAsync(h => h.Id == id);
        return JsonSerializer.Serialize(new { h.Id, h.Name, h.CreatedAt, h.UpdatedAt, h.OwnershipVersion,
            Members = h.Members.OrderBy(m => m.MembershipId).Select(m => new { m.MembershipId, m.PersonId, m.Role }) });
    }

    private async Task<string> AccountStateAsync()
    {
        await using var db = Context();
        return JsonSerializer.Serialize(await db.Users.AsNoTracking().OrderBy(u => u.Id).ToArrayAsync());
    }

    private async Task<string> StateAsync()
    {
        await using var db = Context();
        var ids = await db.Set<Household>().OrderBy(h => h.Id).Select(h => h.Id).ToArrayAsync();
        var homes = new List<string>();
        foreach (var id in ids) homes.Add(await HouseholdStateAsync(id));
        return JsonSerializer.Serialize(new { Homes = homes,
            People = await db.Set<Person>().AsNoTracking().OrderBy(p => p.Id).ToArrayAsync(),
            Accounts = await AccountStateAsync() });
    }

    private async Task AssertNoBrokenReferencesAsync()
    {
        await using var db = Context();
        Assert.False(await db.Set<HouseholdMember>().AnyAsync(m => !db.Set<Person>().Any(p => p.Id == m.PersonId)));
        Assert.False(await db.Set<HouseholdMember>().AnyAsync(m => !db.Set<Household>().Any(h => h.Id == m.HouseholdId)));
        Assert.False(await db.Users.AnyAsync(u => !db.Set<Person>().Any(p => p.Id == u.PersonId)));
    }

    public async Task DisposeAsync()
    {
        _gate.Release.TrySetResult();
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private sealed record Seed(Household Home, Household Other, Person Target, Person UnrelatedPerson, Guid MembershipId);

    private sealed class CleanupGate : DbCommandInterceptor
    {
        private int _remaining;
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Arm(int count) => _remaining = count;
        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("DELETE FROM \"Person\"", StringComparison.Ordinal) && Volatile.Read(ref _remaining) > 0)
            {
                if (Interlocked.Decrement(ref _remaining) == 0) Reached.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }
}

public sealed class RemoveHouseholdMemberRouteTests
{
    [Fact]
    public async Task Anonymous_request_is_rejected_without_database_access()
    {
        await using var factory = new HomePlatformApiFactory(
            "Host=127.0.0.1;Port=1;Database=unused;Username=unused");
        using var client = factory.CreateClient();
        using var response = await client.DeleteAsync($"/api/households/{Guid.NewGuid()}/members/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public async Task Route_is_only_exposed_in_testing(string environment)
    {
        await using var factory = new RestrictedFactory(environment);
        using var client = factory.CreateClient();
        using var response = await client.DeleteAsync($"/api/households/{Guid.NewGuid()}/members/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class RestrictedFactory(string environment) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("ConnectionStrings:Database", "Host=127.0.0.1;Port=1;Database=unused;Username=unused");
        }
    }
}
