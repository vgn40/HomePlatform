using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests;

public sealed class HouseholdPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6-alpine")
        .WithDatabase("homeplatform")
        .WithUsername("homeplatform")
        .WithPassword("homeplatform-dev")
        .Build();

    private DbContextOptions<HomePlatformDbContext>? _options;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _options = new DbContextOptionsBuilder<HomePlatformDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task Household_can_be_saved_cleared_and_reloaded_with_members()
    {
        var ownerAccountId = Guid.NewGuid();
        var household = new Household("Test household", ownerAccountId);
        var owner = Assert.Single(household.Members);
        var expectedHouseholdId = household.Id;
        var expectedName = household.Name;
        var expectedMembershipId = owner.MembershipId;
        var expectedOwnerAccountId = owner.AccountId;
        var expectedOwnerRole = owner.Role;

        await using var context = CreateContext();
        context.Set<Household>().Add(household);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var reloaded = await context.Set<Household>()
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

    [Fact]
    public async Task Duplicate_household_account_is_rejected_for_stale_aggregate_instances()
    {
        var household = new Household("Test household", Guid.NewGuid());

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
        var household = new Household("Test household", Guid.NewGuid());

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
        await _postgres.DisposeAsync();
    }

    private HomePlatformDbContext CreateContext()
    {
        return new HomePlatformDbContext(
            _options ?? throw new InvalidOperationException("Test fixture is not initialized."));
    }

    private static Task<Household> LoadHousehold(
        HomePlatformDbContext context,
        Guid householdId)
    {
        return context.Set<Household>()
            .Include(candidate => candidate.Members)
            .SingleAsync(candidate => candidate.Id == householdId);
    }
}
