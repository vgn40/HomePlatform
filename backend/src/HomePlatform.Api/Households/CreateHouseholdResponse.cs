namespace HomePlatform.Api.Households;

public sealed record CreateHouseholdResponse(
    Guid HouseholdId,
    string Name);