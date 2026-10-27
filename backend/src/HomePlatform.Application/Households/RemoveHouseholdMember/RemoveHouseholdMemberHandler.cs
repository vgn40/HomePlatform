using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Households.RemoveHouseholdMember;

public sealed class RemoveHouseholdMemberHandler
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentPerson _currentPerson;

    public RemoveHouseholdMemberHandler(
        IHouseholdRepository householdRepository,
        ICurrentPerson currentPerson)
    {
        _householdRepository = householdRepository;
        _currentPerson = currentPerson;
    }

    public async Task<RemoveHouseholdMemberResult> Handle(
        RemoveHouseholdMemberCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var personId = await _currentPerson.GetPersonIdAsync(cancellationToken);
        if (personId is null)
        {
            return RemoveHouseholdMemberResult.Unauthenticated();
        }

        var household = await _householdRepository.GetByIdAsync(
            command.HouseholdId,
            cancellationToken);

        if (household is null)
        {
            return RemoveHouseholdMemberResult.NotFound();
        }

        var removeResult = household.RemoveMember(personId.Value, command.MembershipId);

        if (!removeResult.IsSuccess)
        {
            return removeResult.Error switch
            {
                RemoveHouseholdMemberError.CurrentPersonNotMember
                    => RemoveHouseholdMemberResult.NotFound(),

                RemoveHouseholdMemberError.MembershipNotFound
                    => RemoveHouseholdMemberResult.NotFound(),

                RemoveHouseholdMemberError.CurrentPersonNotOwner
                    => RemoveHouseholdMemberResult.Forbidden(),

                RemoveHouseholdMemberError.CannotRemoveSelf
                    => RemoveHouseholdMemberResult.Invalid(
                        "Use LeaveHousehold to leave the household."),

                RemoveHouseholdMemberError.CannotRemoveOwner
                    => RemoveHouseholdMemberResult.Invalid(
                        "An owner cannot be removed through this operation."),

                _ => throw new InvalidOperationException(
                    "Unexpected RemoveHouseholdMember domain error.")
            };
        }

        await _householdRepository.UpdateAsync(
            household,
            cancellationToken);

        return RemoveHouseholdMemberResult.Success();
    }
}
