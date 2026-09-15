using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Persistence;
using HomePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests.Households;

public sealed class LeaveHouseholdPersistenceTests : IAsyncLifetime
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

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Repository_tracked_leave_physically_deletes_only_acting_membership(HouseholdRole role)
    {
        var ownerId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        await _factory.CreateIdentityAccountAsync(ownerId);
        await _factory.CreateIdentityAccountAsync(accountId);
        var household = new Household("Leave persistence", ownerId);
        Assert.True(household.AddMember(role, accountId).IsSuccess);
        Assert.True(household.AddMember(HouseholdRole.Guest).IsSuccess);
        var targetId = household.Members.Single(member => member.AccountId == accountId).MembershipId;
        var before = household.Members.OrderBy(member => member.MembershipId)
            .Select(member => (member.MembershipId, member.AccountId, member.Role)).ToArray();
        await using (var setup = _factory.Services.CreateAsyncScope())
        {
            var db = setup.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
            await new HouseholdRepository(db).AddAsync(household);
        }
        await using (var leaving = _factory.Services.CreateAsyncScope())
        {
            var db = leaving.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
            var repository = new HouseholdRepository(db);
            var loaded = await repository.GetByIdAsync(household.Id);
            Assert.NotNull(loaded);
            Assert.Equal(EntityState.Unchanged, db.Entry(loaded).State);
            Assert.All(loaded.Members, member => Assert.Equal(EntityState.Unchanged, db.Entry(member).State));
            Assert.True(loaded.Leave(accountId).IsSuccess);
            await repository.UpdateAsync(loaded);
        }
        await using var verification = _factory.Services.CreateAsyncScope();
        var context = verification.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        var after = await context.Set<Household>().Include(candidate => candidate.Members)
            .SingleAsync(candidate => candidate.Id == household.Id);
        Assert.Equal(household.Name, after.Name);
        Assert.Equal(before.Length - 1, after.Members.Count);
        Assert.Equal(before.Where(member => member.MembershipId != targetId).ToArray(),
            after.Members.OrderBy(member => member.MembershipId)
                .Select(member => (member.MembershipId, member.AccountId, member.Role)).ToArray());
        Assert.Equal(ownerId, Assert.Single(after.Members, member => member.Role == HouseholdRole.Owner).AccountId);
        Assert.False(await context.Set<HouseholdMember>().AnyAsync(member => member.MembershipId == targetId));
        Assert.Equal(before.Length - 1, await context.Set<HouseholdMember>()
            .CountAsync(member => EF.Property<Guid>(member, "HouseholdId") == household.Id));
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
