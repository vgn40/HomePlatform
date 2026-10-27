namespace HomePlatform.Application.Households.RemoveHouseholdMember;

public sealed record RemoveHouseholdMemberCommand(
    Guid HouseholdId,
    Guid MembershipId);
