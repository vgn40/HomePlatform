using HomePlatform.Application.Households;
using HomePlatform.Domain.Household;
using HomePlatform.Domain.Person;
using HomePlatform.Infrastructure.Persistence;
using HomePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HomePlatform.IntegrationTests.Households;

public sealed class OwnershipVersionMigrationTests : IAsyncLifetime
{
    private const string Previous = "20260921064921_AddPersonFoundation";
    private readonly PostgreSqlTestDatabase _postgres = new();
    public Task InitializeAsync() => _postgres.StartAsync();

    [Fact]
    public async Task Fresh_chain_has_required_uuid_concurrency_token_and_no_pending_changes()
    {
        await using var db = Context();
        await db.Database.MigrateAsync();
        Assert.Equal(db.Database.GetMigrations(), await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
        var token = db.Model.FindEntityType(typeof(Household))!.FindProperty(nameof(Household.OwnershipVersion))!;
        Assert.True(token.IsConcurrencyToken);
        Assert.False(token.IsNullable);
        Assert.Equal(ValueGenerated.Never, token.ValueGenerated);
        Assert.Equal("uuid", token.GetColumnType());
        var column = await db.Database.SqlQueryRaw<ColumnInfo>("""
            SELECT data_type AS "DataType", is_nullable AS "IsNullable"
            FROM information_schema.columns
            WHERE table_name = 'Household' AND column_name = 'OwnershipVersion'
            """).SingleAsync();
        Assert.Equal("uuid", column.DataType);
        Assert.Equal("NO", column.IsNullable);
        var person = new Person("Fresh owner");
        var household = new Household("Fresh home", person.Id);
        db.AddRange(person, household);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var saved = await db.Set<Household>().SingleAsync();
        Assert.NotEqual(Guid.Empty, saved.OwnershipVersion);
        Assert.Equal(household.OwnershipVersion, saved.OwnershipVersion);
    }

    [Fact]
    public async Task Upgrade_preserves_existing_data_and_empty_tokens_still_reject_stale_writes()
    {
        var a = new Person("Existing owner A");
        var b = new Person("Existing owner B");
        var household = new Household("Existing home", a.Id);
        Assert.True(household.AddMember(a.Id, HouseholdRole.Owner, b.Id).IsSuccess);
        var other = new Household("Other existing home", a.Id);
        await using (var db = Context())
        {
            await db.GetService<IMigrator>().MigrateAsync(Previous);
            db.AddRange(a, b);
            await db.SaveChangesAsync();
            // The historical table has no OwnershipVersion column yet.
            foreach (var home in new[] { household, other })
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO "Household" ("Id", "Name", "CreatedAt", "UpdatedAt")
                    VALUES ({home.Id}, {home.Name}, {home.CreatedAt}, {home.UpdatedAt})
                    """);
                foreach (var member in home.Members)
                    await db.Database.ExecuteSqlInterpolatedAsync($"""
                        INSERT INTO "HouseholdMember" ("MembershipId", "HouseholdId", "PersonId", "Role")
                        VALUES ({member.MembershipId}, {home.Id}, {member.PersonId}, {(int)member.Role})
                        """);
            }
            var before = await HistoricalStateAsync(db);
            await db.Database.MigrateAsync();
            Assert.Equal(before, await HistoricalStateAsync(db));
            Assert.False(db.Database.HasPendingModelChanges());
            Assert.All(await db.Set<Household>().ToListAsync(), h => Assert.Equal(Guid.Empty, h.OwnershipVersion));
        }
        await using var first = Context();
        await using var stale = Context();
        var firstRepo = new HouseholdRepository(first);
        var staleRepo = new HouseholdRepository(stale);
        var h1 = (await firstRepo.GetByIdAsync(household.Id))!;
        var h2 = (await staleRepo.GetByIdAsync(household.Id))!;
        Assert.True(h1.Leave(a.Id).IsSuccess);
        Assert.True(h2.Leave(b.Id).IsSuccess);
        await firstRepo.UpdateAsync(h1);
        await Assert.ThrowsAsync<HouseholdConcurrencyException>(() => staleRepo.UpdateAsync(h2));
        await using var verify = Context();
        var saved = await new HouseholdRepository(verify).GetByIdAsync(household.Id);
        Assert.Equal(b.Id, Assert.Single(saved!.Members).PersonId);
        Assert.NotEqual(Guid.Empty, saved.OwnershipVersion);
        Assert.Equal(Guid.Empty, (await verify.Set<Household>().SingleAsync(h => h.Id == other.Id)).OwnershipVersion);
        Assert.Equal(2, await verify.Set<Person>().CountAsync());
    }

    private static async Task<string[]> HistoricalStateAsync(HomePlatformDbContext db)
    {
        // Compare every pre-existing column, including timestamps, memberships and Persons.
        var rows = await db.Database.SqlQueryRaw<string>("""
            SELECT row_to_json(h)::text AS "Value" FROM
              (SELECT "Id", "Name", "CreatedAt", "UpdatedAt" FROM "Household") h
            UNION ALL SELECT row_to_json(m)::text AS "Value" FROM "HouseholdMember" m
            UNION ALL SELECT row_to_json(p)::text AS "Value" FROM "Person" p
            """).ToListAsync();
        return rows.Order().ToArray();
    }

    private HomePlatformDbContext Context() => new(new DbContextOptionsBuilder<HomePlatformDbContext>()
        .UseNpgsql(_postgres.GetConnectionString()).Options);
    public async Task DisposeAsync() => await _postgres.DisposeAsync();
    public sealed class ColumnInfo
    {
        public string DataType { get; set; } = null!;
        public string IsNullable { get; set; } = null!;
    }
}
