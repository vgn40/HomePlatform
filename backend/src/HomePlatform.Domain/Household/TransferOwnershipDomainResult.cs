namespace HomePlatform.Domain.Household;

public sealed class TransferOwnershipDomainResult
{
    public bool IsSuccess => Error is null;
    public TransferOwnershipError? Error { get; }

    private TransferOwnershipDomainResult(TransferOwnershipError? error)
    {
        Error = error;
    }

    public static TransferOwnershipDomainResult Success()
    {
        return new TransferOwnershipDomainResult(null);
    }

    public static TransferOwnershipDomainResult Failure(TransferOwnershipError error)
    {
        return new TransferOwnershipDomainResult(error);
    }
}
