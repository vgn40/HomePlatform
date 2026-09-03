using HomePlatform.Application.Households;
using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Persistence;

namespace HomePlatform.Infrastructure.Persistence.Repositories;

public sealed class HouseholdRepository : IHouseholdRepository
{
    private readonly HomePlatformDbContext _dbContext;

    public HouseholdRepository(HomePlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Household household,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(household);

        _dbContext.Set<Household>().Add(household);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
