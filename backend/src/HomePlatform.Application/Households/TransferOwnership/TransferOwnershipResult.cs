namespace HomePlatform.Application.Households.TransferOwnership;

public sealed record TransferOwnershipResult
{
    public TransferOwnershipOutcome Outcome { get; }
    public string? ErrorMessage { get; }

    private TransferOwnershipResult(
        TransferOwnershipOutcome outcome,
        string? errorMessage = null)
    {
        Outcome = outcome;
        ErrorMessage = errorMessage;
    }

    public static TransferOwnershipResult Success()
    {
        return new TransferOwnershipResult(
            TransferOwnershipOutcome.Success);
    }

    public static TransferOwnershipResult Unauthenticated()
    {
        return new TransferOwnershipResult(
            TransferOwnershipOutcome.Unauthenticated);
    }

    public static TransferOwnershipResult NotFound()
    {
        return new TransferOwnershipResult(
            TransferOwnershipOutcome.NotFound);
    }

    public static TransferOwnershipResult Forbidden()
    {
        return new TransferOwnershipResult(
            TransferOwnershipOutcome.Forbidden);
    }

    public static TransferOwnershipResult Invalid(
        string errorMessage)
    {
        return new TransferOwnershipResult(
            TransferOwnershipOutcome.Invalid,
            errorMessage);
    }
}