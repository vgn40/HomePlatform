using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public sealed class TransferOwnershipDomainResultTests
{
    [Fact]
    public void Success_has_no_error()
    {
        var result = TransferOwnershipDomainResult.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData(TransferOwnershipError.CurrentAccountNotOwner)]
    [InlineData(TransferOwnershipError.NewOwnerNotFound)]
    [InlineData(TransferOwnershipError.NewOwnerHasNoAccount)]
    [InlineData(TransferOwnershipError.CannotTransferToSelf)]
    public void Failure_preserves_exact_error(TransferOwnershipError error)
    {
        var result = TransferOwnershipDomainResult.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }
}
