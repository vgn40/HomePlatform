namespace HomePlatform.Domain.Household;

public enum TransferOwnershipError
{
    CurrentAccountNotOwner,
    NewOwnerNotFound,
    NewOwnerHasNoAccount,
    CannotTransferToSelf,
    CurrentAccountNotMember
}