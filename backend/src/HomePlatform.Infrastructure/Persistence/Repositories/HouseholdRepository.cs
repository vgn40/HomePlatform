using HomePlatform.Application.Households;
using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Household?> GetByIdAsync(
        Guid householdId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<Household>()
            .Include(household => household.Members)
            .SingleOrDefaultAsync(
                household => household.Id == householdId,
                cancellationToken);
    }

    public async Task UpdateAsync(
        Household household,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(household);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}