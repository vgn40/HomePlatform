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

        var currentMember = household.Members.FirstOrDefault(
            member => member.AccountId == accountId);

        if (currentMember is null ||
            currentMember.Role != HouseholdRole.Owner)
        {
            return TransferOwnershipResult.Forbidden();
        }

        var transferResult = household.TransferOwnership(
            accountId,
            command.NewOwnerMembershipId);

        if (!transferResult.IsSuccess)
        {
            return TransferOwnershipResult.Invalid(
                transferResult.ErrorMessage
                ?? "Ownership transfer failed.");
        }

        await _householdRepository.UpdateAsync(
            household,
            cancellationToken);

        return TransferOwnershipResult.Success();
    }
}