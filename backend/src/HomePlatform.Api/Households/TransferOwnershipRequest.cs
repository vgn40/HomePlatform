namespace HomePlatform.Api.Households;

public sealed record TransferOwnershipRequest(
    Guid NewOwnerMembershipId);