using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Households.TransferOwnership;

public sealed class TransferOwnershipHandler
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentAccount _currentAccount;

    public TransferOwnershipHandler(
        IHouseholdRepository householdRepository,
        ICurrentAccount currentAccount)
    {
        _householdRepository = householdRepository;
        _currentAccount = currentAccount;
    }

    public async Task<TransferOwnershipResult> Handle(
        TransferOwnershipCommand command,
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
            return TransferOwnershipResult.Unauthenticated();
        }

        var household = await _householdRepository.GetByIdAsync(
            command.HouseholdId,
            cancellationToken);

        if (household is null)
        {
            return TransferOwnershipResult.NotFound();
        }

        var transferResult = household.TransferOwnership(
            accountId,
            command.NewOwnerMembershipId);

        if (!transferResult.IsSuccess)
        {
            return transferResult.Error switch
            {
                TransferOwnershipError.CurrentAccountNotOwner
                    => TransferOwnershipResult.Forbidden(),
                TransferOwnershipError.NewOwnerNotFound
                    => TransferOwnershipResult.Invalid(
                        "New owner is not a member of this household."),
                TransferOwnershipError.NewOwnerHasNoAccount
                    => TransferOwnershipResult.Invalid(
                        "New owner must be linked to an account."),
                TransferOwnershipError.CannotTransferToSelf
                    => TransferOwnershipResult.Invalid(
                        "Owner cannot transfer ownership to themselves."),
                _ => throw new InvalidOperationException(
                    "Unexpected TransferOwnership domain error.")
            };
        }

        await _householdRepository.UpdateAsync(
            household,
            cancellationToken);

        return TransferOwnershipResult.Success();
    }
}