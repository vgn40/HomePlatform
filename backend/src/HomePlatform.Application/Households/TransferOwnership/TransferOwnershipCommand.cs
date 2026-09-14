namespace HomePlatform.Application.Households.TransferOwnership;

public sealed record TransferOwnershipCommand(
    Guid HouseholdId,
    Guid NewOwnerMembershipId);