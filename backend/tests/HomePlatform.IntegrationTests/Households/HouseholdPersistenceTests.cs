using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Persistence;
using HomePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests.Households;

public sealed class HouseholdPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6-alpine")
        .WithDatabase("homeplatform")
        .WithUsername("homeplatform")
        .WithPassword("homeplatform-dev")
        .Build();

    private DbContextOptions<HomePlatformDbContext>? _options;
    private HomePlatformApiFactory? _factory;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new HomePlatformApiFactory(
            _postgres.GetConnectionString(), useTestAuthentication: false);

        _options = new DbContextOptionsBuilder<HomePlatformDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task Repository_add_persists_and_reloads_household_with_owner()
    {
        var ownerAccountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(ownerAccountId);
        var household = new Household("Test household", ownerAccountId);
        var owner = Assert.Single(household.Members);
        var expectedHouseholdId = household.Id;
        var expectedName = household.Name;
        var expectedMembershipId = owner.MembershipId;
        var expectedOwnerAccountId = owner.AccountId;
        var expectedOwnerRole = owner.Role;

        await using (var writeContext = CreateContext())
        {
            var repository = new HouseholdRepository(writeContext);
            await repository.AddAsync(household);
        }

        await using var verificationContext = CreateContext();
        var reloaded = await verificationContext.Set<Household>()
            .Include(candidate => candidate.Members)
            .SingleAsync(candidate => candidate.Id == expectedHouseholdId);

        Assert.Equal(expectedHouseholdId, reloaded.Id);
        Assert.Equal(expectedName, reloaded.Name);

        var reloadedOwner = Assert.Single(reloaded.Members);
        Assert.Equal(expectedMembershipId, reloadedOwner.MembershipId);
        Assert.Equal(expectedOwnerAccountId, reloadedOwner.AccountId);
        Assert.Equal(expectedOwnerRole, reloadedOwner.Role);
        Assert.Equal(HouseholdRole.Owner, reloadedOwner.Role);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Repository_tracked_transfer_persists_roles_and_preserves_memberships(
        HouseholdRole targetRole)
    {
        var ownerAccountId = Guid.NewGuid();
        var targetAccountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(ownerAccountId);
        await Factory.CreateIdentityAccountAsync(targetAccountId);
        var household = new Household("Ownership transfer", ownerAccountId);
        var ownerMembershipId = Assert.Single(household.Members).MembershipId;
        Assert.True(household.AddMember(targetRole, targetAccountId).IsSuccess);
        Assert.True(household.AddMember(HouseholdRole.Guest).IsSuccess);
        var targetMembershipId = Assert.Single(household.Members,
            member => member.AccountId == targetAccountId).MembershipId;
        var expectedMemberships = household.Members
            .OrderBy(member => member.MembershipId)
            .Select(member => (member.MembershipId, member.AccountId))
            .ToArray();

        await using (var setupContext = CreateContext())
        {
            await new HouseholdRepository(setupContext).AddAsync(household);
        }

        var interceptor = new SaveChangesInvocationInterceptor();
        await using (var transferContext = CreateContext(interceptor))
        {
            var repository = new HouseholdRepository(transferContext);
            var loaded = await repository.GetByIdAsync(household.Id);
            Assert.NotNull(loaded);
            Assert.Equal(expectedMemberships.Length, loaded.Members.Count);
            Assert.Equal(EntityState.Unchanged, transferContext.Entry(loaded).State);
            Assert.All(loaded.Members, member =>
                Assert.Equal(EntityState.Unchanged, transferContext.Entry(member).State));

            var result = loaded.TransferOwnership(ownerAccountId, targetMembershipId);

            Assert.True(result.IsSuccess, result.Error?.ToString());
            await repository.UpdateAsync(loaded);
            Assert.Equal(1, interceptor.AsyncInvocationCount);
        }

        await using var verificationContext = CreateContext();
        var reloaded = await LoadHousehold(verificationContext, household.Id);
        Assert.Equal(expectedMemberships.Length, reloaded.Members.Count);
        Assert.Equal(expectedMemberships, reloaded.Members
            .OrderBy(member => member.MembershipId)
            .Select(member => (member.MembershipId, member.AccountId))
            .ToArray());
        Assert.Equal(HouseholdRole.Member, Assert.Single(reloaded.Members,
            member => member.MembershipId == ownerMembershipId).Role);
        Assert.Equal(HouseholdRole.Owner, Assert.Single(reloaded.Members,
            member => member.MembershipId == targetMembershipId).Role);
        Assert.Equal(HouseholdRole.Guest, Assert.Single(reloaded.Members,
            member => member.AccountId is null).Role);
    }

    [Fact]
    public async Task Repository_add_with_already_cancelled_token_writes_zero_rows()
    {
        var ownerAccountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(ownerAccountId);
        var household = new Household("Cancelled household", ownerAccountId);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await using (var writeContext = CreateContext())
        {
            var repository = new HouseholdRepository(writeContext);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => repository.AddAsync(household, cancellation.Token));
        }

        await AssertAggregateWasNotPersisted(household.Id);
    }

    [Fact]
    public async Task Repository_add_invokes_save_changes_exactly_once()
    {
        var interceptor = new SaveChangesInvocationInterceptor();
        var ownerAccountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(ownerAccountId);
        var household = new Household("Single save household", ownerAccountId);

        await using var context = CreateContext(interceptor);
        var repository = new HouseholdRepository(context);

        await repository.AddAsync(household);

        Assert.Equal(1, interceptor.AsyncInvocationCount);
    }

    [Fact]
    public async Task Repository_add_rolls_back_household_when_owner_insert_fails()
    {
        var ownerAccountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(ownerAccountId);
        var household = new Household("Rollback household", ownerAccountId);

        await using (var setupContext = CreateContext())
        {
            await setupContext.Database.ExecuteSqlRawAsync(
                """
                CREATE FUNCTION fail_household_member_insert()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM "Household"
                        WHERE "Id" = NEW."HouseholdId"
                    ) THEN
                        RAISE EXCEPTION 'household parent was not inserted first';
                    END IF;

                    RAISE EXCEPTION 'forced household member insert failure';
                END;
                $$;

                CREATE TRIGGER fail_household_member_insert
                BEFORE INSERT ON "HouseholdMember"
                FOR EACH ROW
                EXECUTE FUNCTION fail_household_member_insert();
                """);
        }

        await using (var writeContext = CreateContext())
        {
            var repository = new HouseholdRepository(writeContext);
            var exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => repository.AddAsync(household));

            var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
            Assert.Equal(PostgresErrorCodes.RaiseException, postgresException.SqlState);
            Assert.Equal(
                "forced household member insert failure",
                postgresException.MessageText);
        }

        await AssertAggregateWasNotPersisted(household.Id);
    }

    [Fact]
    public async Task Duplicate_household_account_is_rejected_for_stale_aggregate_instances()
    {
        var ownerAccountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(ownerAccountId);
        var household = new Household("Test household", ownerAccountId);

        await using (var setupContext = CreateContext())
        {
            setupContext.Set<Household>().Add(household);
            await setupContext.SaveChangesAsync();
        }

        await using var context1 = CreateContext();
        await using var context2 = CreateContext();

        var household1 = await LoadHousehold(context1, household.Id);
        var household2 = await LoadHousehold(context2, household.Id);
        var accountB = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(accountB);

        Assert.True(household1.AddMember(HouseholdRole.Member, accountB).IsSuccess);
        Assert.True(household2.AddMember(HouseholdRole.Member, accountB).IsSuccess);

        await context1.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => context2.SaveChangesAsync());

        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresException.SqlState);
    }

    [Fact]
    public async Task Multiple_loginless_members_are_allowed_in_the_same_household()
    {
        var ownerAccountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(ownerAccountId);
        var household = new Household("Test household", ownerAccountId);

        Assert.True(household.AddMember(HouseholdRole.Member).IsSuccess);
        Assert.True(household.AddMember(HouseholdRole.Member).IsSuccess);

        await using var context = CreateContext();
        context.Set<Household>().Add(household);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var reloaded = await LoadHousehold(context, household.Id);

        Assert.Equal(2, reloaded.Members.Count(member => member.AccountId is null));
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    private HomePlatformApiFactory Factory =>
        _factory ?? throw new InvalidOperationException("Test fixture is not initialized.");

    private HomePlatformDbContext CreateContext(
        params IInterceptor[] interceptors)
    {
        var options = _options
            ?? throw new InvalidOperationException("Test fixture is not initialized.");

        if (interceptors.Length > 0)
        {
            options = new DbContextOptionsBuilder<HomePlatformDbContext>(options)
                .AddInterceptors(interceptors)
                .Options;
        }

        return new HomePlatformDbContext(options);
    }

    private async Task AssertAggregateWasNotPersisted(Guid householdId)
    {
        await using var verificationContext = CreateContext();

        Assert.False(await verificationContext.Set<Household>()
            .AnyAsync(candidate => candidate.Id == householdId));
        Assert.False(await verificationContext.Set<HouseholdMember>()
            .AnyAsync(member => EF.Property<Guid>(member, "HouseholdId") == householdId));
    }

    private static Task<Household> LoadHousehold(
        HomePlatformDbContext context,
        Guid householdId)
    {
        return context.Set<Household>()
            .Include(candidate => candidate.Members)
            .SingleAsync(candidate => candidate.Id == householdId);
    }

    private sealed class SaveChangesInvocationInterceptor : SaveChangesInterceptor
    {
        public int AsyncInvocationCount { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            AsyncInvocationCount++;
            return base.SavingChangesAsync(
                eventData,
                result,
                cancellationToken);
        }
    }
}
