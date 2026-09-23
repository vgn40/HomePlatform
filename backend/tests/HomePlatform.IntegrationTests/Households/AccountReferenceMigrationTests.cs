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
        await InsertHistoricalAccountAsync(accountId);
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
                context.Database.ExecuteSqlInterpolatedAsync($"""DELETE FROM "AspNetUsers" WHERE "Id" = {accountId}"""));
            AssertAccountReferenceViolation(failure);
        }

        await AssertExistingDataAsync(household, accountId);
    }

    [Fact]
    public async Task Upgrade_rejects_dangling_reference_without_repairing_or_deleting_existing_data()
    {
        var ownerAccountId = Guid.NewGuid();
        await InsertHistoricalAccountAsync(ownerAccountId);
        var danglingAccountId = Guid.NewGuid();
        var household = new Household("Existing dangling reference", ownerAccountId);
        Assert.True(household.AddMember(household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId, HouseholdRole.Member, danglingAccountId).IsSuccess);
        await PersistHouseholdAsync(household);
        await AssertExistingDataAsync(household, ownerAccountId);

        string[] appliedBefore;
        await using (var context = CreateContext())
        {
            appliedBefore = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
            Assert.False(await AccountExistsAsync(context, danglingAccountId));

            var failure = await Assert.ThrowsAsync<PostgresException>(() =>
                context.GetService<IMigrator>().MigrateAsync(AccountReferenceMigration));
            AssertAccountReferenceViolation(failure);
        }

        // Use a fresh context after the failed DDL transaction. Neither migration
        // history nor the existing membership/account data may be silently repaired.
        await using (var verification = CreateContext())
        {
            Assert.Equal(appliedBefore, await verification.Database.GetAppliedMigrationsAsync());
            Assert.False(await AccountExistsAsync(verification, danglingAccountId));
        }

        await AssertExistingDataAsync(household, ownerAccountId);
    }

    // Historical schema tests use SQL: the current EF model intentionally no longer
    // maps AccountId on memberships and cannot materialize a pre-Person database.
    private async Task InsertHistoricalAccountAsync(Guid id)
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "AspNetUsers" ("Id", "EmailConfirmed", "PhoneNumberConfirmed",
                "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
            VALUES ({id}, false, false, false, false, 0)
            """);
    }

    private static async Task<bool> AccountExistsAsync(HomePlatformDbContext context, Guid id)
        => await context.Database.SqlQuery<Guid>($"""SELECT "Id" AS "Value" FROM "AspNetUsers" WHERE "Id" = {id}""").AnyAsync();

    private async Task PersistHouseholdAsync(Household household)
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "Household" ("Id", "Name", "CreatedAt", "UpdatedAt")
            VALUES ({household.Id}, {household.Name}, {household.CreatedAt}, {household.UpdatedAt})
            """);
        foreach (var member in household.Members)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "HouseholdMember" ("MembershipId", "HouseholdId", "AccountId", "Role")
                VALUES ({member.MembershipId}, {household.Id}, {member.PersonId}, {(int)member.Role})
                """);
        }
    }

    private async Task AssertExistingDataAsync(Household expected, Guid existingAccountId)
    {
        await using var context = CreateContext();
        Assert.Equal(existingAccountId, await context.Database.SqlQueryRaw<Guid>(
            """SELECT "Id" AS "Value" FROM "AspNetUsers" """).SingleAsync());
        var rows = await context.Database.SqlQueryRaw<HistoricalMember>(
            """SELECT "MembershipId", "HouseholdId", "AccountId", "Role" FROM "HouseholdMember" """).ToListAsync();
        Assert.Equal(expected.Members.Count, rows.Count);
        foreach (var member in expected.Members)
        {
            var row = Assert.Single(rows, r => r.MembershipId == member.MembershipId);
            Assert.Equal(expected.Id, row.HouseholdId);
            Assert.Equal(member.PersonId, row.AccountId);
            Assert.Equal((int)member.Role, row.Role);
        }
    }

    public sealed class HistoricalMember
    {
        public Guid MembershipId { get; set; }
        public Guid HouseholdId { get; set; }
        public Guid? AccountId { get; set; }
        public int Role { get; set; }
    }

    [Fact]
    public async Task Person_upgrade_preserves_linked_and_loginless_memberships_and_downgrades()
    {
        var accountId = Guid.NewGuid();
        var unusedAccountId = Guid.NewGuid();
        await InsertHistoricalAccountAsync(accountId);
        await InsertHistoricalAccountAsync(unusedAccountId);
        var first = new Household("Existing home", accountId);
        var second = new Household("Second home", accountId);
        await PersistHouseholdAsync(first);
        await PersistHouseholdAsync(second);
        var loginlessIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        await using var context = CreateContext();
        foreach (var id in loginlessIds)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "HouseholdMember" ("MembershipId", "HouseholdId", "AccountId", "Role")
                VALUES ({id}, {first.Id}, NULL, {(int)HouseholdRole.Guest})
                """);
        }
        await context.Database.MigrateAsync();
        var users = await context.Users.ToListAsync();
        Assert.Equal(2, users.Count);
        Assert.Equal(2, users.Select(u => u.PersonId).Distinct().Count());
        Assert.All(users, u => Assert.NotEqual(u.Id, u.PersonId));
        var people = await context.Set<HomePlatform.Domain.Person.Person>().ToListAsync();
        Assert.Equal(4, people.Count);
        Assert.All(people, p => Assert.Null(p.DisplayName));
        var members = await context.Set<HouseholdMember>().ToListAsync();
        Assert.Equal(4, members.Count);
        foreach (var original in new[] { first, second })
        {
            var owner = Assert.Single(members, m => m.MembershipId == original.Members.Single().MembershipId);
            Assert.Equal(users.Single(u => u.Id == accountId).PersonId, owner.PersonId);
            Assert.Equal(HouseholdRole.Owner, owner.Role);
            Assert.Equal(original.Id, context.Entry(owner).Property<Guid>("HouseholdId").CurrentValue);
        }
        var loginless = members.Where(m => loginlessIds.Contains(m.MembershipId)).ToArray();
        Assert.Equal(2, loginless.Select(m => m.PersonId).Distinct().Count());
        Assert.All(loginless, m =>
        {
            Assert.Equal(HouseholdRole.Guest, m.Role);
            Assert.Equal(first.Id, context.Entry(m).Property<Guid>("HouseholdId").CurrentValue);
            Assert.DoesNotContain(users, u => u.PersonId == m.PersonId);
        });
        context.ChangeTracker.Clear();
        await context.GetService<IMigrator>().MigrateAsync(AccountReferenceMigration);
        var restored = await context.Database.SqlQueryRaw<HistoricalMember>(
            """SELECT "MembershipId", "HouseholdId", "AccountId", "Role" FROM "HouseholdMember" """).ToListAsync();
        Assert.Equal(4, restored.Count);
        Assert.All(restored, m => Assert.Equal(loginlessIds.Contains(m.MembershipId) ? (Guid?)null : accountId, m.AccountId));
        await context.Database.MigrateAsync();
        Assert.Equal(4, await context.Set<HouseholdMember>().CountAsync());
        await context.Users.Where(u => u.Id == accountId).ExecuteDeleteAsync();
        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        var failure = await Assert.ThrowsAsync<PostgresException>(() =>
            context.GetService<IMigrator>().MigrateAsync(AccountReferenceMigration));
        Assert.Contains("owner Person has no account", failure.MessageText);
        Assert.Equal(applied, await context.Database.GetAppliedMigrationsAsync());
        Assert.Equal(4, await context.Set<HouseholdMember>().CountAsync());
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
