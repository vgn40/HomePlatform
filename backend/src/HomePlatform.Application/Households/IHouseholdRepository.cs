using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Households;

public interface IHouseholdRepository
{
    Task AddAsync(
        Household household,
        CancellationToken cancellationToken = default);
}