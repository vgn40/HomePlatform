namespace HomePlatform.Application.Accounts.Refresh;

public interface IAccountRefresh
{
    Task<AccountRefreshResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);
}