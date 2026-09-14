using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests.Households;

public sealed class AccountReferenceMigrationTests : IAsyncLifetime
{
    private const string PreviousMigration = "20260904100319_AddIdentityPersistence";
    private const string AccountReferenceMigration = "20260913183105_AddHouseholdMemberAccountReference";
    private const string AccountReferenceConstraint = "FK_HouseholdMember_AspNetUsers_AccountId";

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

        await using var context = CreateContext();
        var migrations = context.Database.GetMigrations().ToArray();
        var targetIndex = Array.IndexOf(migrations, AccountReferenceMigration);
        Assert.True(targetIndex > 0);
        Assert.Equal(PreviousMigration, migrations[targetIndex - 1]);

        // Seed historical rows before the new FK exists, using the real migration chain.
        await context.GetService<IMigrator>().MigrateAsync(PreviousMigration);
        Assert.Equal(migrations.Take(targetIndex), await context.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task Upgrade_preserves_valid_account_reference_and_enforces_foreign_key()
    {
        var accountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(accountId);
        var household = new Household("Existing valid household", accountId);
        await PersistHouseholdAsync(household);
        await AssertExistingDataAsync(household, accountId);

        await using (var context = CreateContext())
        {
            await context.GetService<IMigrator>().MigrateAsync(AccountReferenceMigration);
            Assert.Equal(AccountReferenceMigration,
                (await context.Database.GetAppliedMigrationsAsync()).Last());
        }

        await AssertExistingDataAsync(household, accountId);

        // A direct DELETE proves the upgraded database enforces the named FK,
        // independently of EF's tracked relationship behavior.
        await using (var context = CreateContext())
        {
            var failure = await Assert.ThrowsAsync<PostgresException>(() =>
                context.Users.Where(user => user.Id == accountId).ExecuteDeleteAsync());
            AssertAccountReferenceViolation(failure);
        }

        await AssertExistingDataAsync(household, accountId);
    }

    [Fact]
    public async Task Upgrade_rejects_dangling_reference_without_repairing_or_deleting_existing_data()
    {
        var ownerAccountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(ownerAccountId);
        var danglingAccountId = Guid.NewGuid();
        var household = new Household("Existing dangling reference", ownerAccountId);
        Assert.True(household.AddMember(HouseholdRole.Member, danglingAccountId).IsSuccess);
        await PersistHouseholdAsync(household);
        await AssertExistingDataAsync(household, ownerAccountId);

        string[] appliedBefore;
        await using (var context = CreateContext())
        {
            appliedBefore = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
            Assert.False(await context.Users.AnyAsync(user => user.Id == danglingAccountId));

            var failure = await Assert.ThrowsAsync<PostgresException>(() =>
                context.GetService<IMigrator>().MigrateAsync(AccountReferenceMigration));
            AssertAccountReferenceViolation(failure);
        }

        // Use a fresh context after the failed DDL transaction. Neither migration
        // history nor the existing membership/account data may be silently repaired.
        await using (var verification = CreateContext())
        {
            Assert.Equal(appliedBefore, await verification.Database.GetAppliedMigrationsAsync());
            Assert.False(await verification.Users.AnyAsync(user => user.Id == danglingAccountId));
        }

        await AssertExistingDataAsync(household, ownerAccountId);
    }

    private async Task PersistHouseholdAsync(Household household)
    {
        await using var context = CreateContext();
        context.Set<Household>().Add(household);
        await context.SaveChangesAsync();
    }

    private async Task AssertExistingDataAsync(Household expected, Guid existingAccountId)
    {
        await using var verification = CreateContext();
        // Exactly the seeded Identity account remains; no replacement account was invented.
        Assert.Equal(existingAccountId, (await verification.Users.SingleAsync()).Id);
        var actual = await verification.Set<Household>().Include(household => household.Members).SingleAsync();
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Members.Count, actual.Members.Count);

        foreach (var expectedMember in expected.Members)
        {
            var actualMember = Assert.Single(actual.Members,
                member => member.MembershipId == expectedMember.MembershipId);
            Assert.Equal(expectedMember.AccountId, actualMember.AccountId);
            Assert.Equal(expectedMember.Role, actualMember.Role);
        }
    }

    private static void AssertAccountReferenceViolation(PostgresException failure)
    {
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, failure.SqlState);
        Assert.Equal(AccountReferenceConstraint, failure.ConstraintName);
    }

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
