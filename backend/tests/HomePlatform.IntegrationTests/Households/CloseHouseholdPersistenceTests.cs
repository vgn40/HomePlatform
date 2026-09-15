using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Persistence;
using HomePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests.Households;

public sealed class CloseHouseholdPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6-alpine")
        .WithDatabase("homeplatform")
        .WithUsername("homeplatform")
        .WithPassword("homeplatform-dev")
        .Build();
    private HomePlatformApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new HomePlatformApiFactory(_postgres.GetConnectionString(), useTestAuthentication: false);
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>().Database.MigrateAsync();
    }

    [Fact]
    public async Task Repository_tracked_close_physically_deletes_aggregate_and_children_but_preserves_accounts_and_other_household()
    {
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        await _factory.CreateIdentityAccountAsync(ownerId);
        await _factory.CreateIdentityAccountAsync(memberId);
        var household = new Household("Close persistence", ownerId);
        Assert.True(household.AddMember(HouseholdRole.Member, memberId).IsSuccess);
        Assert.True(household.AddMember(HouseholdRole.Guest).IsSuccess);
        var membershipIds = household.Members.Select(m => m.MembershipId).ToArray();
        var unrelated = new Household("Unrelated home", memberId);
        Assert.True(unrelated.AddMember(HouseholdRole.Member, ownerId).IsSuccess);
        Assert.True(unrelated.AddMember(HouseholdRole.Guest).IsSuccess);
        var unrelatedMembers = unrelated.Members.OrderBy(m => m.MembershipId)
            .Select(m => (m.MembershipId, m.AccountId, m.Role)).ToArray();
        await using (var setup = _factory.Services.CreateAsyncScope())
        {
            var db = setup.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
            var repository = new HouseholdRepository(db);
            await repository.AddAsync(household);
            await repository.AddAsync(unrelated);
        }
        await using (var closing = _factory.Services.CreateAsyncScope())
        {
            var db = closing.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
            var repository = new HouseholdRepository(db);
            var loaded = await repository.GetByIdAsync(household.Id);
            Assert.NotNull(loaded);
            Assert.Equal(EntityState.Unchanged, db.Entry(loaded).State);
            Assert.Equal(membershipIds.Length, loaded.Members.Count);
            Assert.All(loaded.Members, m => Assert.Equal(EntityState.Unchanged, db.Entry(m).State));
            Assert.True(loaded.Close(ownerId).IsSuccess);
            await repository.DeleteAsync(loaded);
        }
        await using var verification = _factory.Services.CreateAsyncScope();
        var context = verification.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        Assert.False(await context.Set<Household>().AnyAsync(h => h.Id == household.Id));
        Assert.False(await context.Set<HouseholdMember>().AnyAsync(m => EF.Property<Guid>(m, "HouseholdId") == household.Id));
        Assert.False(await context.Set<HouseholdMember>().AnyAsync(m => membershipIds.Contains(m.MembershipId)));
        Assert.True(await context.Users.AnyAsync(u => u.Id == ownerId));
        Assert.True(await context.Users.AnyAsync(u => u.Id == memberId));
        var remaining = await context.Set<Household>().Include(h => h.Members).SingleAsync();
        Assert.Equal(unrelated.Id, remaining.Id);
        Assert.Equal(unrelated.Name, remaining.Name);
        Assert.Equal(unrelated.CreatedAt, remaining.CreatedAt);
        Assert.Equal(unrelated.UpdatedAt, remaining.UpdatedAt);
        Assert.Equal(unrelatedMembers, remaining.Members.OrderBy(m => m.MembershipId)
            .Select(m => (m.MembershipId, m.AccountId, m.Role)).ToArray());
        Assert.Equal(unrelatedMembers.Length, await context.Set<HouseholdMember>().CountAsync());
    }

    [Fact]
    public async Task Repository_delete_null_throws_without_removing_rows()
    {
        var ownerId = Guid.NewGuid();
        await _factory.CreateIdentityAccountAsync(ownerId);
        var household = new Household("Keep home", ownerId);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var repository = new HouseholdRepository(scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>());
            await repository.AddAsync(household);
            await Assert.ThrowsAsync<ArgumentNullException>(() => repository.DeleteAsync(null!));
        }
        await using var verification = _factory.Services.CreateAsyncScope();
        var db = verification.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        Assert.True(await db.Set<Household>().AnyAsync(h => h.Id == household.Id));
        Assert.True(await db.Set<HouseholdMember>().AnyAsync(m => m.MembershipId == household.Members.Single().MembershipId));
        Assert.True(await db.Users.AnyAsync(u => u.Id == ownerId));
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
