using HomePlatform.Application.Accounts.Register;
using HomePlatform.Application.Identity;
using HomePlatform.Application.Households.CreateHousehold;
using HomePlatform.Application.Households.TransferOwnership;
using HomePlatform.Application.Households.LeaveHousehold;
using HomePlatform.Application.Households.CloseHousehold;
using Microsoft.Extensions.DependencyInjection;
using HomePlatform.Domain.Person;
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
    public async Task Loginless_membership_persists_with_required_PersonId(HouseholdRole role)
    {
        var (household, owner) = await CreateLinkedHouseholdAsync(HouseholdRole.Owner);
        var loginlessPersonId = await Factory.CreateLoginlessPersonAsync();
        Guid membershipId;

        await using (var context = CreateContext())
        {
            var tracked = await LoadHouseholdAsync(context, household.Id);
            Assert.True(tracked.AddMember(role, loginlessPersonId).IsSuccess);
            membershipId = Assert.Single(tracked.Members,
                candidate => candidate.PersonId == loginlessPersonId).MembershipId;
            await context.SaveChangesAsync();
        }

        await using var verification = CreateContext();
        var reloaded = await LoadHouseholdAsync(verification, household.Id);
        var loginless = Assert.Single(reloaded.Members,
            candidate => candidate.MembershipId == membershipId);
        Assert.Equal(loginlessPersonId, loginless.PersonId);
        Assert.False(await verification.Users.AnyAsync(user => user.PersonId == loginlessPersonId));
        Assert.Equal(role, loginless.Role);
        Assert.Equal(2, reloaded.Members.Count);
        Assert.Equal(owner.PersonId, (await verification.Users.SingleAsync()).Id);
    }

    [Fact]
    public async Task Unknown_PersonId_is_rejected_by_PostgreSQL_without_committing_membership()
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
                candidate => candidate.PersonId == unknownAccountId).MembershipId;

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
    public async Task Direct_person_delete_with_untracked_membership_is_rejected_by_PostgreSQL(
        HouseholdRole role)
    {
        var (household, member) = await CreateLinkedHouseholdAsync(role);

        await using (var context = CreateContext())
        {
            var user = await context.Set<Person>().SingleAsync(candidate => candidate.Id == member.PersonId);
            Assert.Empty(context.ChangeTracker.Entries<Household>());
            Assert.Empty(context.ChangeTracker.Entries<HouseholdMember>());

            context.Remove(user);
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
    public async Task Direct_person_delete_with_tracked_membership_is_rejected_without_mutating_membership(
        HouseholdRole role)
    {
        var (household, member) = await CreateLinkedHouseholdAsync(role);

        await using (var context = CreateContext())
        {
            var user = await context.Set<Person>().SingleAsync(candidate => candidate.Id == member.PersonId);
            var trackedHousehold = await LoadHouseholdAsync(context, household.Id);
            var trackedMember = Assert.Single(trackedHousehold.Members,
                candidate => candidate.MembershipId == member.MembershipId);
            Assert.Equal(EntityState.Unchanged, context.Entry(user).State);
            Assert.Equal(EntityState.Unchanged, context.Entry(trackedMember).State);

            // EF may reject while processing tracked relationships, or PostgreSQL may reject
            // the DELETE. Either way, direct deletion must leave the membership intact.
            var failure = await Record.ExceptionAsync(async () =>
            {
                context.Remove(user);
                await context.SaveChangesAsync();
            });

            Assert.NotNull(failure);
            Assert.True(failure is DbUpdateException or InvalidOperationException,
                $"Expected a persistence/relationship rejection, got {failure.GetType().Name}: {failure.Message}");
            if (failure is DbUpdateException databaseFailure)
            {
                AssertForeignKeyViolation(databaseFailure);
            }

            Assert.Equal(member.PersonId, trackedMember.PersonId);
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
            context.Remove(user);
            await context.SaveChangesAsync();
        }

        await using var verification = CreateContext();
        Assert.False(await verification.Users.AnyAsync(candidate => candidate.Id == accountId));
        Assert.Empty(await verification.Set<HouseholdMember>().ToListAsync());
        Assert.True(await verification.Set<Person>().AnyAsync(p => p.Id == accountId));
    }

    [Fact]
    public void Model_maps_required_PersonId_foreign_key_to_Person_Id()
    {
        using var context = CreateContext();
        var memberType = context.Model.FindEntityType(typeof(HouseholdMember));
        Assert.NotNull(memberType);
        var foreignKey = Assert.Single(memberType.GetForeignKeys(), candidate =>
            candidate.PrincipalEntityType.ClrType == typeof(Person));

        Assert.Equal(nameof(HouseholdMember.PersonId), Assert.Single(foreignKey.Properties).Name);
        Assert.Equal(nameof(Person.Id), Assert.Single(foreignKey.PrincipalKey.Properties).Name);
        Assert.True(foreignKey.IsRequired);
        Assert.False(Assert.Single(foreignKey.Properties).IsNullable);
    }

    [Fact]
    public async Task Registration_and_lifecycle_resolve_distinct_account_and_person_ids()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var registration = scope.ServiceProvider.GetRequiredService<IAccountRegistration>();
        await registration.RegisterAsync("owner@example.com", "ValidPassword123!", default);
        await registration.RegisterAsync("member@example.com", "ValidPassword123!", default);
        var db = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        var users = await db.Users.OrderBy(u => u.Email).ToArrayAsync();
        Assert.Equal(2, users.Length);
        var owner = users.Single(u => u.Email == "owner@example.com");
        var member = users.Single(u => u.Email == "member@example.com");
        Assert.All(users, u => Assert.NotEqual(u.Id, u.PersonId));
        Assert.Equal(2, await db.Set<Person>().CountAsync());
        var resolver = scope.ServiceProvider.GetRequiredService<IAccountPersonLookup>();
        Assert.Equal(owner.PersonId, await resolver.GetPersonIdAsync(owner.Id));
        Assert.Null(await resolver.GetPersonIdAsync(Guid.NewGuid()));
        var repository = new HomePlatform.Infrastructure.Persistence.Repositories.HouseholdRepository(db);
        var created = await new CreateHouseholdHandler(repository, new IdentityCurrentPerson(new CurrentAccount(owner.Id), resolver))
            .Handle(new CreateHouseholdCommand("Resolved home"));
        Assert.Equal(CreateHouseholdOutcome.Success, created.Outcome);
        var household = await db.Set<Household>().Include(h => h.Members).SingleAsync();
        Assert.Equal(owner.PersonId, Assert.Single(household.Members).PersonId);
        Assert.True(household.AddMember(HouseholdRole.Member, member.PersonId).IsSuccess);
        var loginless = new Person();
        db.Set<Person>().Add(loginless);
        Assert.True(household.AddMember(HouseholdRole.Guest, loginless.Id).IsSuccess);
        await db.SaveChangesAsync();
        Assert.True(await resolver.HasAccountForPersonAsync(owner.PersonId));
        Assert.False(await resolver.HasAccountForPersonAsync(loginless.Id));
        var target = household.Members.Single(m => m.PersonId == member.PersonId);
        var transfer = new TransferOwnershipHandler(repository, new IdentityCurrentPerson(new CurrentAccount(owner.Id), resolver), resolver);
        var invalid = await transfer.Handle(new TransferOwnershipCommand(household.Id,
            household.Members.Single(m => m.PersonId == loginless.Id).MembershipId));
        Assert.Equal(TransferOwnershipOutcome.Invalid, invalid.Outcome);
        Assert.Equal(HouseholdRole.Owner, household.Members.Single(m => m.PersonId == owner.PersonId).Role);
        var transferred = await transfer.Handle(new TransferOwnershipCommand(household.Id, target.MembershipId));
        Assert.Equal(TransferOwnershipOutcome.Success, transferred.Outcome);
        var left = await new LeaveHouseholdHandler(repository, new IdentityCurrentPerson(new CurrentAccount(owner.Id), resolver))
            .Handle(new LeaveHouseholdCommand(household.Id));
        Assert.Equal(LeaveHouseholdOutcome.Success, left.Outcome);
        var closed = await new CloseHouseholdHandler(repository, new IdentityCurrentPerson(new CurrentAccount(member.Id), resolver))
            .Handle(new CloseHouseholdCommand(household.Id));
        Assert.Equal(CloseHouseholdOutcome.Success, closed.Outcome);
        db.ChangeTracker.Clear();
        Assert.Empty(await db.Set<Household>().ToListAsync());
        Assert.Equal(3, await db.Set<Person>().CountAsync());
        Assert.Equal(2, await db.Users.CountAsync());
    }

    [Fact]
    public async Task Person_cannot_have_two_accounts_and_account_deletion_preserves_membership()
    {
        var (household, member) = await CreateLinkedHouseholdAsync(HouseholdRole.Owner);
        await using (var db = CreateContext())
        {
            db.Users.Add(new ApplicationUser { Id = Guid.NewGuid(), PersonId = member.PersonId });
            var failure = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(failure.InnerException).SqlState);
        }
        await using (var db = CreateContext())
        {
            await db.Users.Where(u => u.PersonId == member.PersonId).ExecuteDeleteAsync();
        }
        await using var verification = CreateContext();
        Assert.True(await verification.Set<Person>().AnyAsync(p => p.Id == member.PersonId));
        Assert.Equal(member.PersonId, Assert.Single((await LoadHouseholdAsync(verification, household.Id)).Members).PersonId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Failed_registration_does_not_leave_person_or_account(bool databaseFailure)
    {
        if (databaseFailure)
        {
            await using var setup = CreateContext();
            await setup.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION reject_account_insert() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'forced account failure'; END; $$;
                CREATE TRIGGER reject_account_insert BEFORE INSERT ON "AspNetUsers"
                FOR EACH ROW EXECUTE FUNCTION reject_account_insert();
                """);
        }
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var registration = scope.ServiceProvider.GetRequiredService<IAccountRegistration>();
            if (databaseFailure)
            {
                await Assert.ThrowsAsync<DbUpdateException>(() => registration.RegisterAsync(
                    "failed@example.com", "ValidPassword123!", default));
            }
            else
            {
                await registration.RegisterAsync("failed@example.com", "short", default);
                // A later save in the same scope must not persist the rejected Person.
                await scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>().SaveChangesAsync();
            }
        }
        await using var verification = CreateContext();
        Assert.Empty(await verification.Users.ToListAsync());
        Assert.Empty(await verification.Set<Person>().ToListAsync());
    }

    private sealed class CurrentAccount(Guid id) : ICurrentAccount
    {
        public Guid AccountId => id;
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
        return (household, Assert.Single(household.Members, candidate => candidate.PersonId == accountId));
    }

    private async Task AssertAccountAndMembershipRemainAsync(Household household, HouseholdMember member)
    {
        await using var verification = CreateContext();
        var user = await verification.Users.SingleAsync(candidate => candidate.Id == member.PersonId);
        var reloaded = await LoadHouseholdAsync(verification, household.Id);
        Assert.Equal(household.Name, reloaded.Name);
        Assert.Equal(household.Members.Count, reloaded.Members.Count);

        foreach (var expected in household.Members)
        {
            var actual = Assert.Single(reloaded.Members,
                candidate => candidate.MembershipId == expected.MembershipId);
            Assert.Equal(expected.PersonId, actual.PersonId);
            Assert.Equal(expected.Role, actual.Role);
        }

        Assert.Equal(user.Id, Assert.Single(reloaded.Members,
            candidate => candidate.MembershipId == member.MembershipId).PersonId);
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
