using HomePlatform.Application.Households.GetHousehold;
using HomePlatform.Application.Households.ListHouseholds;

namespace HomePlatform.Application.Households;

public interface IHouseholdQueryService
{
    Task<IReadOnlyList<HouseholdSummaryResponse>> ListForPersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default);

    Task<HouseholdDetailResponse?> GetForPersonAsync(
        Guid householdId,
        Guid personId,
        CancellationToken cancellationToken = default);
}
