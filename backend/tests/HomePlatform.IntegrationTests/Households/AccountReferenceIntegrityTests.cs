using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Identity;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests.Households;

public sealed class AccountReferenceIntegrityTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine")
            .WithDatabase("homeplatform")
            .WithUsername("homeplatform")
            .WithPassword("homeplatform-dev")
            .Build();

    private HomePlatformApiFactory? _factory;
    private DbContextOptions<HomePlatformDbContext>? _options;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new HomePlatformApiFactory(
            _postgres.GetConnectionString(), useTestAuthentication: false);
        _options = new DbContextOptionsBuilder<HomePlatformDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        // Exercise the schema produced by production migrations from an empty database.
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    [Fact]
    public async Task Membership_with_real_Identity_account_persists_and_reloads()
    {
        var (household, member) = await CreateLinkedHouseholdAsync(HouseholdRole.Owner);

        await AssertAccountAndMembershipRemainAsync(household, member);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Loginless_membership_persists_with_null_AccountId(HouseholdRole role)
    {
        var (household, owner) = await CreateLinkedHouseholdAsync(HouseholdRole.Owner);
        Guid membershipId;

        await using (var context = CreateContext())
        {
            var tracked = await LoadHouseholdAsync(context, household.Id);
            Assert.True(tracked.AddMember(role, accountId: null).IsSuccess);
            membershipId = Assert.Single(tracked.Members,
                candidate => candidate.AccountId is null).MembershipId;
            await context.SaveChangesAsync();
        }

        await using var verification = CreateContext();
        var reloaded = await LoadHouseholdAsync(verification, household.Id);
        var loginless = Assert.Single(reloaded.Members,
            candidate => candidate.MembershipId == membershipId);
        Assert.Null(loginless.AccountId);
        Assert.Equal(role, loginless.Role);
        Assert.Equal(2, reloaded.Members.Count);
        Assert.Equal(owner.AccountId, (await verification.Users.SingleAsync()).Id);
    }

    [Fact]
    public async Task Unknown_AccountId_is_rejected_by_PostgreSQL_without_committing_membership()
    {
        var (household, owner) = await CreateLinkedHouseholdAsync(HouseholdRole.Owner);
        var unknownAccountId = Guid.NewGuid();
        Guid invalidMembershipId;

        await using (var context = CreateContext())
        {
            Assert.False(await context.Users.AnyAsync(user => user.Id == unknownAccountId));
            var tracked = await LoadHouseholdAsync(context, household.Id);
            Assert.True(tracked.AddMember(HouseholdRole.Member, unknownAccountId).IsSuccess);
            invalidMembershipId = Assert.Single(tracked.Members,
                candidate => candidate.AccountId == unknownAccountId).MembershipId;

            var failure = await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync());
            AssertForeignKeyViolation(failure);
        }

        await using var verification = CreateContext();
        Assert.False(await verification.Set<HouseholdMember>()
            .AnyAsync(candidate => candidate.MembershipId == invalidMembershipId));
        var reloaded = await LoadHouseholdAsync(verification, household.Id);
        Assert.Equal(owner.MembershipId, Assert.Single(reloaded.Members).MembershipId);
        await AssertAccountAndMembershipRemainAsync(household, owner);
    }

    [Theory]
    [InlineData(HouseholdRole.Owner)]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Direct_account_delete_with_untracked_membership_is_rejected_by_PostgreSQL(
        HouseholdRole role)
    {
        var (household, member) = await CreateLinkedHouseholdAsync(role);

        await using (var context = CreateContext())
        {
            var user = await context.Users.SingleAsync(candidate => candidate.Id == member.AccountId);
            Assert.Empty(context.ChangeTracker.Entries<Household>());
            Assert.Empty(context.ChangeTracker.Entries<HouseholdMember>());

            context.Users.Remove(user);
            var failure = await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync());
            AssertForeignKeyViolation(failure);
        }

        await AssertAccountAndMembershipRemainAsync(household, member);
    }

    [Theory]
    [InlineData(HouseholdRole.Owner)]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Direct_account_delete_with_tracked_membership_is_rejected_without_mutating_membership(
        HouseholdRole role)
    {
        var (household, member) = await CreateLinkedHouseholdAsync(role);

        await using (var context = CreateContext())
        {
            var user = await context.Users.SingleAsync(candidate => candidate.Id == member.AccountId);
            var trackedHousehold = await LoadHouseholdAsync(context, household.Id);
            var trackedMember = Assert.Single(trackedHousehold.Members,
                candidate => candidate.MembershipId == member.MembershipId);
            Assert.Equal(EntityState.Unchanged, context.Entry(user).State);
            Assert.Equal(EntityState.Unchanged, context.Entry(trackedMember).State);

            // EF may reject while processing tracked relationships, or PostgreSQL may reject
            // the DELETE. Either way, direct deletion must leave the membership intact.
            var failure = await Record.ExceptionAsync(async () =>
            {
                context.Users.Remove(user);
                await context.SaveChangesAsync();
            });

            Assert.NotNull(failure);
            Assert.True(failure is DbUpdateException or InvalidOperationException,
                $"Expected a persistence/relationship rejection, got {failure.GetType().Name}: {failure.Message}");
            if (failure is DbUpdateException databaseFailure)
            {
                AssertForeignKeyViolation(databaseFailure);
            }

            Assert.Equal(member.AccountId, trackedMember.AccountId);
            Assert.DoesNotContain(context.Entry(trackedMember).State,
                new[] { EntityState.Deleted, EntityState.Detached });
        }

        // A fresh context observes durable state independently of EF's identity map.
        await AssertAccountAndMembershipRemainAsync(household, member);
    }

    [Fact]
    public async Task Account_without_memberships_can_be_deleted()
    {
        var accountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(accountId);

        await using (var context = CreateContext())
        {
            Assert.Empty(await context.Set<HouseholdMember>().ToListAsync());
            var user = await context.Users.SingleAsync(candidate => candidate.Id == accountId);
            context.Users.Remove(user);
            await context.SaveChangesAsync();
        }

        await using var verification = CreateContext();
        Assert.False(await verification.Users.AnyAsync(candidate => candidate.Id == accountId));
        Assert.Empty(await verification.Set<HouseholdMember>().ToListAsync());
    }

    [Fact]
    public void Model_maps_optional_AccountId_foreign_key_to_Identity_user_Id()
    {
        using var context = CreateContext();
        var memberType = context.Model.FindEntityType(typeof(HouseholdMember));
        Assert.NotNull(memberType);
        var foreignKey = Assert.Single(memberType.GetForeignKeys(), candidate =>
            candidate.PrincipalEntityType.ClrType == typeof(ApplicationUser));

        Assert.Equal(nameof(HouseholdMember.AccountId), Assert.Single(foreignKey.Properties).Name);
        Assert.Equal(nameof(ApplicationUser.Id), Assert.Single(foreignKey.PrincipalKey.Properties).Name);
        Assert.False(foreignKey.IsRequired);
        Assert.True(Assert.Single(foreignKey.Properties).IsNullable);
    }

    private async Task<(Household Household, HouseholdMember Member)> CreateLinkedHouseholdAsync(
        HouseholdRole role)
    {
        var accountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(accountId);
        var ownerAccountId = accountId;
        if (role != HouseholdRole.Owner)
        {
            ownerAccountId = Guid.NewGuid();
            await Factory.CreateIdentityAccountAsync(ownerAccountId);
        }

        var household = new Household("Account reference household", ownerAccountId);
        if (role != HouseholdRole.Owner)
        {
            Assert.True(household.AddMember(role, accountId).IsSuccess);
        }

        await using var context = CreateContext();
        context.Set<Household>().Add(household);
        await context.SaveChangesAsync();
        return (household, Assert.Single(household.Members, candidate => candidate.AccountId == accountId));
    }

    private async Task AssertAccountAndMembershipRemainAsync(Household household, HouseholdMember member)
    {
        await using var verification = CreateContext();
        var user = await verification.Users.SingleAsync(candidate => candidate.Id == member.AccountId);
        var reloaded = await LoadHouseholdAsync(verification, household.Id);
        Assert.Equal(household.Name, reloaded.Name);
        Assert.Equal(household.Members.Count, reloaded.Members.Count);

        foreach (var expected in household.Members)
        {
            var actual = Assert.Single(reloaded.Members,
                candidate => candidate.MembershipId == expected.MembershipId);
            Assert.Equal(expected.AccountId, actual.AccountId);
            Assert.Equal(expected.Role, actual.Role);
        }

        Assert.Equal(user.Id, Assert.Single(reloaded.Members,
            candidate => candidate.MembershipId == member.MembershipId).AccountId);
    }

    private static void AssertForeignKeyViolation(DbUpdateException failure)
    {
        var postgresFailure = Assert.IsType<PostgresException>(failure.InnerException);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgresFailure.SqlState);
    }

    private static Task<Household> LoadHouseholdAsync(HomePlatformDbContext context, Guid householdId) =>
        context.Set<Household>().Include(candidate => candidate.Members)
            .SingleAsync(candidate => candidate.Id == householdId);

    private HomePlatformDbContext CreateContext() =>
        new(_options ?? throw new InvalidOperationException("Test fixture is not initialized."));

    private HomePlatformApiFactory Factory =>
        _factory ?? throw new InvalidOperationException("Test fixture is not initialized.");

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }
}
