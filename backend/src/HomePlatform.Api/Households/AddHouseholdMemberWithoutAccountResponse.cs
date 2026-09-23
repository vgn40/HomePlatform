namespace HomePlatform.Api.Households;

public sealed record AddHouseholdMemberWithoutAccountResponse(
    Guid PersonId,
    Guid MembershipId);