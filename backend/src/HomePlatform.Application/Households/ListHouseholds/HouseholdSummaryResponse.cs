using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Households.ListHouseholds;

public sealed record HouseholdSummaryResponse(
    Guid HouseholdId,
    string Name,
    HouseholdRole Role,
    int MemberCount);
