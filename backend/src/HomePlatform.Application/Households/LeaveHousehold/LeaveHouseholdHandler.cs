using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Households.LeaveHousehold;

public sealed class LeaveHouseholdHandler
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentPerson _currentPerson;

    public LeaveHouseholdHandler(
        IHouseholdRepository householdRepository,
        ICurrentPerson currentPerson)
    {
        _householdRepository = householdRepository;
        _currentPerson = currentPerson;
    }

    public async Task<LeaveHouseholdResult> Handle(
        LeaveHouseholdCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var personId = await _currentPerson.GetPersonIdAsync(cancellationToken);
        if (personId is null)
        {
            return LeaveHouseholdResult.Unauthenticated();
        }

        var household = await _householdRepository.GetByIdAsync(
            command.HouseholdId,
            cancellationToken);

        if (household is null)
        {
            return LeaveHouseholdResult.NotFound();
        }

        var leaveResult = household.Leave(personId.Value);

        if (!leaveResult.IsSuccess)
        {
            return leaveResult.Error switch
            {
                LeaveHouseholdError.CurrentPersonNotMember
                    => LeaveHouseholdResult.NotFound(),

                LeaveHouseholdError.OwnerCannotLeave
                    => LeaveHouseholdResult.Invalid(
                        "Owner must transfer ownership or close the household before leaving."),

                _ => throw new InvalidOperationException(
                    "Unexpected LeaveHousehold domain error.")
            };
        }

        await _householdRepository.UpdateAsync(
            household,
            cancellationToken);

        return LeaveHouseholdResult.Success();
    }
}
