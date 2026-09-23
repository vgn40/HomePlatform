using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Households.AddHouseholdMemberWithoutAccount;

public sealed record AddHouseholdMemberWithoutAccountCommand(
    Guid HouseholdId,
    string DisplayName,
    HouseholdRole Role);