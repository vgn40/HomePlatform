using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Households.TransferOwnership;

public sealed class TransferOwnershipHandler
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentPerson _currentPerson;
    private readonly IAccountPersonLookup _accountPersonLookup;

    public TransferOwnershipHandler(
        IHouseholdRepository householdRepository,
        ICurrentPerson currentPerson,
        IAccountPersonLookup accountPersonLookup)
    {
        _householdRepository = householdRepository;
        _currentPerson = currentPerson;
        _accountPersonLookup = accountPersonLookup;
    }

    public async Task<TransferOwnershipResult> Handle(
        TransferOwnershipCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var personId = await _currentPerson.GetPersonIdAsync(cancellationToken);
        if (personId is null)
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

        var transferResult = household.ValidateTransferOwnership(
            personId.Value,
            command.NewOwnerMembershipId);

        if (!transferResult.IsSuccess)
        {
            return transferResult.Error switch
            {
                TransferOwnershipError.CurrentPersonNotMember
                    => TransferOwnershipResult.NotFound(),

                TransferOwnershipError.CurrentPersonNotOwner
                    => TransferOwnershipResult.Forbidden(),

                TransferOwnershipError.NewOwnerNotFound
                    => TransferOwnershipResult.Invalid(
                        "New owner is not a member of this household."),

                TransferOwnershipError.CannotTransferToSelf
                    => TransferOwnershipResult.Invalid(
                        "Owner cannot transfer ownership to themselves."),

                _ => throw new InvalidOperationException(
                    "Unexpected TransferOwnership domain error.")
            };
        }

        var target = household.Members.Single(member => member.MembershipId == command.NewOwnerMembershipId);
        if (!await _accountPersonLookup.HasAccountForPersonAsync(target.PersonId, cancellationToken))
        {
            return TransferOwnershipResult.Invalid("New owner must be linked to an account.");
        }

        household.TransferOwnership(personId.Value, command.NewOwnerMembershipId);

        await _householdRepository.UpdateAsync(
            household,
            cancellationToken);

        return TransferOwnershipResult.Success();
    }
}
