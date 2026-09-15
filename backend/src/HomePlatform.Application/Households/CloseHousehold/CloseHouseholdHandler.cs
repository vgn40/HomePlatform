using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Households.CloseHousehold;

public sealed class CloseHouseholdHandler
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentAccount _currentAccount;

    public CloseHouseholdHandler(
        IHouseholdRepository householdRepository,
        ICurrentAccount currentAccount)
    {
        _householdRepository = householdRepository;
        _currentAccount = currentAccount;
    }

    public async Task<CloseHouseholdResult> Handle(
        CloseHouseholdCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        Guid accountId;

        try
        {
            accountId = _currentAccount.AccountId;
        }
        catch (UnauthorizedAccessException)
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

        var closeResult = household.Close(accountId);

        if (!closeResult.IsSuccess)
        {
            return closeResult.Error switch
            {
                CloseHouseholdError.CurrentAccountNotOwner
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