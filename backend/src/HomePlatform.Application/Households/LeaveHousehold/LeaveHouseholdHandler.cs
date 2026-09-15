using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Households.LeaveHousehold;

public sealed class LeaveHouseholdHandler
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentAccount _currentAccount;

    public LeaveHouseholdHandler(
        IHouseholdRepository householdRepository,
        ICurrentAccount currentAccount)
    {
        _householdRepository = householdRepository;
        _currentAccount = currentAccount;
    }

    public async Task<LeaveHouseholdResult> Handle(
        LeaveHouseholdCommand command,
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
            return LeaveHouseholdResult.Unauthenticated();
        }

        var household = await _householdRepository.GetByIdAsync(
            command.HouseholdId,
            cancellationToken);

        if (household is null)
        {
            return LeaveHouseholdResult.NotFound();
        }

        var leaveResult = household.Leave(accountId);

        if (!leaveResult.IsSuccess)
        {
            return leaveResult.Error switch
            {
                LeaveHouseholdError.CurrentAccountNotMember
                    => LeaveHouseholdResult.Forbidden(),

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