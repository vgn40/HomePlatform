namespace HomePlatform.Application.Households.CreateHousehold;

public sealed record CreateHouseholdResult(
    Guid HouseholdId,
    string Name);