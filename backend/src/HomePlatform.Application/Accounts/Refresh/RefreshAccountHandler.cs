namespace HomePlatform.Application.Accounts.Refresh;

public sealed class RefreshAccountHandler(
    IAccountRefresh accountRefresh,
    RefreshAccountValidator validator)
{
    public Task<AccountRefreshResult> Handle(
        RefreshAccountCommand command,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(command);

        if (errors.Count > 0)
        {
            return Task.FromResult(
                AccountRefreshResult.Failure(
                    errors.ToArray()));
        }

        return accountRefresh.RefreshAsync(
            command.RefreshToken,
            cancellationToken);
    }
}