using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Households;

public interface IHouseholdRepository
{
    Task AddAsync(
        Household household,
        CancellationToken cancellationToken = default);

    Task<Household?> GetByIdAsync(
        Guid householdId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Household household,
        CancellationToken cancellationToken = default);
}