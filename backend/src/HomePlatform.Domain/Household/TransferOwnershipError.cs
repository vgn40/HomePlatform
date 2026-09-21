namespace HomePlatform.Domain.Household;

public enum TransferOwnershipError
{
    CurrentPersonNotOwner,
    NewOwnerNotFound,
    CannotTransferToSelf,
    CurrentPersonNotMember
}
