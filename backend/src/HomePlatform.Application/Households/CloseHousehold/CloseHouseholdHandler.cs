using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Households.CloseHousehold;

public sealed class CloseHouseholdHandler
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentPerson _currentPerson;

    public CloseHouseholdHandler(
        IHouseholdRepository householdRepository,
        ICurrentPerson currentPerson)
    {
        _householdRepository = householdRepository;
        _currentPerson = currentPerson;
    }

    public async Task<CloseHouseholdResult> Handle(
        CloseHouseholdCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var personId = await _currentPerson.GetPersonIdAsync(cancellationToken);
        if (personId is null)
        {
            return CloseHouseholdResult.Unauthenticated();
        }

        var household = await _householdRepository.GetByIdAsync(
            command.HouseholdId,
            cancellationToken);

        if (household is null)
        {
            return CloseHouseholdResult.NotFound();
        }

        var closeResult = household.Close(personId.Value);

        if (!closeResult.IsSuccess)
        {
            return closeResult.Error switch
            {
                CloseHouseholdError.CurrentPersonNotMember
                    => CloseHouseholdResult.NotFound(),

                CloseHouseholdError.CurrentPersonNotOwner
                    => CloseHouseholdResult.Forbidden(),

                _ => throw new InvalidOperationException(
                    "Unexpected CloseHousehold domain error.")
            };
        }

        await _householdRepository.DeleteAsync(
            household,
            cancellationToken);

        return CloseHouseholdResult.Success();
    }
}
