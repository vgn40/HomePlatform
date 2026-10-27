using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomePlatform.Application.Households;
using HomePlatform.Domain.Household;
using HomePlatform.Domain.Person;
using HomePlatform.Infrastructure.Persistence;
using HomePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HomePlatform.IntegrationTests.Households;

public sealed class HouseholdConcurrencyTests : IAsyncLifetime
{
    private readonly PostgreSqlTestDatabase _postgres = new();
    private readonly WriteGate _gate = new();
    private readonly SqlRecorder _sql = new();
    private HomePlatformApiFactory _factory = null!;
    private HttpClient _client = null!;
    private readonly Guid _a = Guid.NewGuid();
    private readonly Guid _b = Guid.NewGuid();
    private readonly Guid _c = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new HomePlatformApiFactory(_postgres.GetConnectionString(), services =>
            services.AddDbContext<HomePlatformDbContext>(options => options.AddInterceptors(_gate, _sql)));
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>().Database.MigrateAsync();
        _client = _factory.CreateClient();
        foreach (var id in new[] { _a, _b, _c })
            await _factory.CreateIdentityAccountAsync(id);
    }

    // The stale HTTP request stops immediately before SaveChanges. The competing
    // HTTP request uses its own scope/DbContext and commits before it is released.
    [Theory]
    [InlineData("two-owners-leave")]
    [InlineData("two-transfers")]
    [InlineData("transfer-then-owner-leave")]
    [InlineData("owner-leave-then-transfer")]
    [InlineData("add-member-transfer")]
    [InlineData("add-guest-transfer")]
    [InlineData("add-member-close")]
    [InlineData("add-guest-close")]
    [InlineData("member-leave-promoted")]
    [InlineData("guest-leave-promoted")]
    [InlineData("transfer-target-left")]
    [InlineData("close-transfer")]
    [InlineData("transfer-close")]
    [InlineData("remove-promoted")]
    [InlineData("remove-transfer")]
    [InlineData("remove-close")]
    [InlineData("remove-remove")]
    [InlineData("transfer-removed")]
    [InlineData("close-add")]
    public async Task Stale_write_returns_409_and_preserves_entire_winning_state(string scenario)
    {
        var household = await SeedAsync(
            secondOwner: scenario is "two-owners-leave" or "transfer-then-owner-leave" or "owner-leave-then-transfer",
            guest: scenario == "guest-leave-promoted");
        var bMembership = household.Members.Single(m => m.PersonId == _b).MembershipId;
        var cMembership = household.Members.Single(m => m.PersonId == _c).MembershipId;
        var (staleAction, staleActor, staleTarget, winnerAction, winnerActor, winnerTarget) = scenario switch
        {
            "two-owners-leave" => ("leave", _b, Guid.Empty, "leave", _a, Guid.Empty),
            "two-transfers" => ("transfer", _a, cMembership, "transfer", _a, bMembership),
            "transfer-then-owner-leave" => ("leave", _b, Guid.Empty, "transfer", _a, cMembership),
            "owner-leave-then-transfer" => ("transfer", _a, cMembership, "leave", _b, Guid.Empty),
            "add-member-transfer" => ("add", _a, Guid.Empty, "transfer", _a, bMembership),
            "add-guest-transfer" => ("guest", _a, Guid.Empty, "transfer", _a, bMembership),
            "add-member-close" => ("add", _a, Guid.Empty, "close", _a, Guid.Empty),
            "add-guest-close" => ("guest", _a, Guid.Empty, "close", _a, Guid.Empty),
            "member-leave-promoted" or "guest-leave-promoted" => ("leave", _b, Guid.Empty, "transfer", _a, bMembership),
            "transfer-target-left" => ("transfer", _a, bMembership, "leave", _b, Guid.Empty),
            "close-transfer" => ("close", _a, Guid.Empty, "transfer", _a, bMembership),
            "transfer-close" => ("transfer", _a, bMembership, "close", _a, Guid.Empty),
            "remove-promoted" => ("remove", _a, bMembership, "transfer", _a, bMembership),
            "remove-transfer" => ("remove", _a, bMembership, "transfer", _a, cMembership),
            "remove-close" => ("remove", _a, bMembership, "close", _a, Guid.Empty),
            "remove-remove" => ("remove", _a, bMembership, "remove", _a, bMembership),
            "transfer-removed" => ("transfer", _a, bMembership, "remove", _a, bMembership),
            "close-add" => ("close", _a, Guid.Empty, "add", _a, Guid.Empty),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        _gate.Arm(household.Id);
        using var staleRequest = Request(household.Id, staleAction, staleActor, staleTarget);
        var pending = _client.SendAsync(staleRequest);
        string winningState;
        try
        {
            await _gate.Reached.Task.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.Equal(household.OwnershipVersion, _gate.OriginalVersion);
            using var winnerRequest = Request(household.Id, winnerAction, winnerActor, winnerTarget);
            using var winner = await _client.SendAsync(winnerRequest);
            Assert.Equal(winnerAction == "add" ? HttpStatusCode.Created : HttpStatusCode.NoContent, winner.StatusCode);
            winningState = await StateAsync();
        }
        finally
        {
            _gate.Release.TrySetResult();
        }
        using var stale = await pending;
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal("application/problem+json", stale.Content.Headers.ContentType?.MediaType);
        var problem = await stale.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal(409, problem!.Status);
        Assert.Equal("Household concurrency conflict", problem.Title);
        Assert.DoesNotContain("Npgsql", await stale.Content.ReadAsStringAsync());
        Assert.Equal(winningState, await StateAsync());
        await using var verification = Context();
        var remaining = await verification.Set<Household>().Include(h => h.Members)
            .SingleOrDefaultAsync(h => h.Id == household.Id);
        if (winnerAction == "close")
        {
            Assert.Null(remaining);
            Assert.False(await verification.Set<HouseholdMember>()
                .AnyAsync(m => m.HouseholdId == household.Id));
        }
        else
        {
            Assert.NotNull(remaining);
            Assert.Contains(remaining.Members, m => m.Role == HouseholdRole.Owner);
        }
        if (_gate.AddedPersonId is Guid newPerson)
        {
            Assert.False(await verification.Set<Person>().AnyAsync(p => p.Id == newPerson));
            Assert.False(await verification.Set<HouseholdMember>().AnyAsync(m => m.PersonId == newPerson));
        }
    }

    [Theory]
    [InlineData("add")]
    [InlineData("leave")]
    [InlineData("close")]
    public async Task Distinct_membership_changes_can_coexist_without_rotating_ownership(string operation)
    {
        var household = await SeedAsync();
        await using var first = Context();
        await using var second = Context();
        var firstRepo = new HouseholdRepository(first);
        var secondRepo = new HouseholdRepository(second);
        var h1 = (await firstRepo.GetByIdAsync(household.Id))!;
        var h2 = (await secondRepo.GetByIdAsync(household.Id))!;
        var person1 = new Person("First");
        Assert.True(h1.AddMember(_a, HouseholdRole.Member, person1.Id).IsSuccess);
        await new AddHouseholdMemberWithoutAccountPersistence(first).SaveAsync(person1, h1);
        if (operation == "add")
        {
            var person2 = new Person("Second");
            Assert.True(h2.AddMember(_a, HouseholdRole.Guest, person2.Id).IsSuccess);
            await new AddHouseholdMemberWithoutAccountPersistence(second).SaveAsync(person2, h2);
        }
        else if (operation == "leave")
        {
            Assert.True(h2.Leave(_b).IsSuccess);
            await secondRepo.UpdateAsync(h2);
        }
        else
        {
            Assert.True(h2.Close(_a).IsSuccess);
            await secondRepo.DeleteAsync(h2);
        }
        await using var verification = Context();
        var saved = await verification.Set<Household>().Include(h => h.Members)
            .SingleOrDefaultAsync(h => h.Id == household.Id);
        Assert.Equal(operation != "close",
            await verification.Set<Person>().AnyAsync(p => p.Id == person1.Id));
        if (operation == "close")
        {
            Assert.Null(saved);
            Assert.False(await verification.Set<HouseholdMember>()
                .AnyAsync(m => m.HouseholdId == household.Id));
            return;
        }
        Assert.NotNull(saved);
        Assert.Equal(household.OwnershipVersion, saved.OwnershipVersion);
        Assert.Contains(saved.Members, m => m.PersonId == person1.Id);
        Assert.Equal(operation == "add" ? 5 : 3, saved.Members.Count);
        Assert.Equal(_a, Assert.Single(saved.Members, m => m.Role == HouseholdRole.Owner).PersonId);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("leave")]
    public async Task Unchanged_timestamp_still_checks_original_token_and_only_updates_required_columns(string operation)
    {
        var household = await SeedAsync();
        await using var context = Context(_sql);
        var repository = new HouseholdRepository(context);
        var loaded = (await repository.GetByIdAsync(household.Id))!;
        var originalTimestamp = loaded.UpdatedAt;
        var person = new Person("Precise tracking");
        if (operation == "add")
            Assert.True(loaded.AddMember(_a, HouseholdRole.Member, person.Id).IsSuccess);
        else
            Assert.True(loaded.Leave(_b).IsSuccess);
        // Model the equal-clock-value case deterministically, without clock delays.
        context.Entry(loaded).Property(h => h.UpdatedAt).CurrentValue = originalTimestamp;
        _sql.Commands.Clear();
        if (operation == "add")
            await new AddHouseholdMemberWithoutAccountPersistence(context).SaveAsync(person, loaded);
        else
            await repository.UpdateAsync(loaded);
        var update = Assert.Single(_sql.Commands, c => c.Text.Contains("UPDATE \"Household\""));
        Assert.Contains("SET \"UpdatedAt\" =", update.Text);
        Assert.Contains("AND \"OwnershipVersion\" =", update.Text);
        Assert.Contains(household.OwnershipVersion, update.Values);
        Assert.DoesNotContain("\"Name\" =", update.Text);
        Assert.DoesNotContain("\"CreatedAt\" =", update.Text);
        Assert.DoesNotContain("UPDATE \"HouseholdMember\"", update.Text);
        Assert.All(loaded.Members, m => Assert.Equal(EntityState.Unchanged, context.Entry(m).State));
    }

    [Theory]
    [InlineData("add")]
    [InlineData("leave")]
    public async Task Equal_timestamp_cannot_bypass_stale_ownership_check(string operation)
    {
        var household = await SeedAsync();
        await using var stale = Context();
        var staleRepo = new HouseholdRepository(stale);
        var loaded = (await staleRepo.GetByIdAsync(household.Id))!;
        var original = loaded.UpdatedAt;
        var person = new Person("Equal timestamp");
        if (operation == "add")
            Assert.True(loaded.AddMember(_a, HouseholdRole.Member, person.Id).IsSuccess);
        else
            Assert.True(loaded.Leave(_b).IsSuccess);
        stale.Entry(loaded).Property(h => h.UpdatedAt).CurrentValue = original;
        using var winnerRequest = Request(household.Id, "transfer", _a,
            household.Members.Single(m => m.PersonId == _b).MembershipId);
        using var winner = await _client.SendAsync(winnerRequest);
        Assert.Equal(HttpStatusCode.NoContent, winner.StatusCode);
        var winningState = await StateAsync();
        await Assert.ThrowsAsync<HouseholdConcurrencyException>(() => operation == "add"
            ? new AddHouseholdMemberWithoutAccountPersistence(stale).SaveAsync(person, loaded)
            : staleRepo.UpdateAsync(loaded));
        Assert.Equal(winningState, await StateAsync());
    }

    [Fact]
    public async Task Delete_checks_original_ownership_token()
    {
        var household = await SeedAsync();
        await using var context = Context(_sql);
        var repository = new HouseholdRepository(context);
        var loaded = (await repository.GetByIdAsync(household.Id))!;
        _sql.Commands.Clear();
        await repository.DeleteAsync(loaded);
        var delete = Assert.Single(_sql.Commands, c => c.Text.Contains("DELETE FROM \"Household\""));
        Assert.Contains("AND \"OwnershipVersion\" =", delete.Text);
        Assert.Contains(household.OwnershipVersion, delete.Values);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unrelated_database_failure_remains_500_and_rolls_back(bool triggerFailure)
    {
        var household = await SeedAsync();
        var before = await StateAsync();
        // Change only the new child's PersonId at the save boundary to provoke a
        // real PostgreSQL FK failure through the actual HTTP/persistence path.
        if (triggerFailure)
        {
            await using var db = Context();
            await db.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION reject_member() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Unexpected insert failure'; END; $$;
                CREATE TRIGGER reject_member BEFORE INSERT ON "HouseholdMember"
                FOR EACH ROW EXECUTE FUNCTION reject_member();
                """);
        }
        else
            _gate.CorruptNewPersonReference = true;
        using var request = Request(household.Id, "add", _a);
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(before, await StateAsync());
        var failure = Assert.IsType<PostgresException>(_sql.Failure);
        Assert.Equal(triggerFailure ? PostgresErrorCodes.RaiseException : PostgresErrorCodes.ForeignKeyViolation,
            failure.SqlState);
        if (!triggerFailure)
            Assert.Equal("FK_HouseholdMember_Person_PersonId", failure.ConstraintName);
        Assert.DoesNotContain("Unexpected insert failure", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain("FK_HouseholdMember_Person_PersonId", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("add")]
    [InlineData("leave")]
    [InlineData("transfer")]
    [InlineData("close")]
    public async Task Cancelled_persistence_is_not_translated_and_writes_nothing(string operation)
    {
        var household = await SeedAsync(secondOwner: true);
        var before = await StateAsync();
        await using var context = Context();
        var repository = new HouseholdRepository(context);
        var loaded = (await repository.GetByIdAsync(household.Id))!;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> save;
        if (operation == "add")
        {
            var person = new Person("Cancelled");
            Assert.True(loaded.AddMember(_a, HouseholdRole.Guest, person.Id).IsSuccess);
            save = () => new AddHouseholdMemberWithoutAccountPersistence(context)
                .SaveAsync(person, loaded, cancellation.Token);
        }
        else if (operation == "close")
            save = () => repository.DeleteAsync(loaded, cancellation.Token);
        else
        {
            if (operation == "leave")
                Assert.True(loaded.Leave(_b).IsSuccess);
            else
                Assert.True(loaded.TransferOwnership(_a,
                    loaded.Members.Single(m => m.PersonId == _c).MembershipId).IsSuccess);
            save = () => repository.UpdateAsync(loaded, cancellation.Token);
        }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(save);
        Assert.Equal(before, await StateAsync());
    }

    private async Task<Household> SeedAsync(bool secondOwner = false, bool guest = false)
    {
        var household = new Household("Concurrency", _a);
        Assert.True(household.AddMember(_a, secondOwner ? HouseholdRole.Owner :
            guest ? HouseholdRole.Guest : HouseholdRole.Member, _b).IsSuccess);
        Assert.True(household.AddMember(_a, HouseholdRole.Member, _c).IsSuccess);
        // Include another aggregate in all full-state rollback comparisons.
        await using var context = Context();
        context.AddRange(household, new Household("Unrelated home", _c));
        await context.SaveChangesAsync();
        return household;
    }

    private HomePlatformDbContext Context(params IInterceptor[] interceptors) => new(
        new DbContextOptionsBuilder<HomePlatformDbContext>()
            .UseNpgsql(_postgres.GetConnectionString()).AddInterceptors(interceptors).Options);

    private async Task<string> StateAsync()
    {
        await using var db = Context();
        return JsonSerializer.Serialize(new
        {
            Households = (await db.Set<Household>().AsNoTracking().Include(h => h.Members).ToListAsync())
                .OrderBy(h => h.Id).Select(h => new { h.Id, h.Name, h.CreatedAt, h.UpdatedAt,
                    h.OwnershipVersion, Members = h.Members.OrderBy(m => m.MembershipId)
                        .Select(m => new { m.MembershipId, m.PersonId, m.Role }) }),
            People = (await db.Set<Person>().AsNoTracking().ToListAsync()).OrderBy(p => p.Id),
            Accounts = await db.Users.AsNoTracking().OrderBy(u => u.Id).Select(u => new { u.Id, u.PersonId }).ToListAsync()
        });
    }

    private static HttpRequestMessage Request(Guid householdId, string operation, Guid actor, Guid target = default)
    {
        var (method, suffix) = operation switch
        {
            "add" or "guest" => (HttpMethod.Post, "/members/without-account"),
            "transfer" => (HttpMethod.Put, "/ownership"),
            "leave" => (HttpMethod.Delete, "/membership"),
            "close" => (HttpMethod.Delete, ""),
            "remove" => (HttpMethod.Delete, $"/members/{target}"),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };
        var request = new HttpRequestMessage(method, $"/api/households/{householdId}{suffix}");
        request.Headers.Add(TestAuthenticationHandler.AccountIdHeaderName, actor.ToString());
        if (operation is "add" or "guest")
            request.Content = JsonContent.Create(new { displayName = "New person", role = operation == "guest" ? "Guest" : "Member" });
        if (operation == "transfer")
            request.Content = JsonContent.Create(new { newOwnerMembershipId = target });
        return request;
    }

    public async Task DisposeAsync()
    {
        _gate.Release.TrySetResult();
        _client?.Dispose();
        if (_factory is not null) await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private sealed class WriteGate : SaveChangesInterceptor
    {
        private Guid _householdId;
        private int _armed;
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Guid OriginalVersion { get; private set; }
        public Guid? AddedPersonId { get; private set; }
        public bool CorruptNewPersonReference { get; set; }

        public void Arm(Guid householdId)
        {
            _householdId = householdId;
            _armed = 1;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var db = eventData.Context!;
            if (CorruptNewPersonReference)
            {
                var member = db.ChangeTracker.Entries<HouseholdMember>().SingleOrDefault(e => e.State == EntityState.Added);
                if (member is not null)
                    member.Property(m => m.PersonId).CurrentValue = Guid.NewGuid();
            }
            var household = db.ChangeTracker.Entries<Household>()
                .SingleOrDefault(e => e.Entity.Id == _householdId);
            if (household is not null && Interlocked.CompareExchange(ref _armed, 0, 1) == 1)
            {
                OriginalVersion = household.Property(h => h.OwnershipVersion).OriginalValue;
                AddedPersonId = db.ChangeTracker.Entries<Person>().SingleOrDefault(e => e.State == EntityState.Added)?.Entity.Id;
                Reached.TrySetResult();
                await Release.Task.WaitAsync(cancellationToken);
            }
            return result;
        }
    }

    private sealed class SqlRecorder : DbCommandInterceptor
    {
        public ConcurrentQueue<(string Text, object?[] Values)> Commands { get; } = new();
        public Exception? Failure { get; private set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Enqueue((command.CommandText, command.Parameters.Cast<DbParameter>().Select(p => p.Value).ToArray()));
            return ValueTask.FromResult(result);
        }
        public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            Failure = eventData.Exception;
            return Task.CompletedTask;
        }
    }
}
