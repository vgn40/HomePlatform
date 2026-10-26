using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Households.GetHousehold;

public sealed record HouseholdDetailResponse(
    Guid HouseholdId,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    HouseholdRole Role,
    IReadOnlyList<HouseholdMemberResponse> Members);

public sealed record HouseholdMemberResponse(
    Guid MembershipId,
    Guid PersonId,
    string? DisplayName,
    HouseholdRole Role);
