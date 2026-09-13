using HomePlatform.Application.Accounts.Refresh;

namespace HomePlatform.Application.Tests.Accounts;

public sealed class RefreshAccountHandlerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public async Task Missing_or_blank_token_returns_required_error_without_calling_refresh(string? token)
    {
        var refresh = new RecordingRefresh();
        var handler = new RefreshAccountHandler(refresh, new RefreshAccountValidator());

        var result = await handler.Handle(new RefreshAccountCommand(token!), default);

        Assert.False(result.Succeeded);
        Assert.Equal(AccountRefreshErrorCode.RefreshTokenRequired, Assert.Single(result.Errors).Code);
        Assert.Equal(0, refresh.Calls);
    }

    [Fact]
    public async Task Non_blank_token_is_forwarded_unchanged_with_cancellation_and_success_result()
    {
        using var cancellation = new CancellationTokenSource();
        var refresh = new RecordingRefresh();
        var handler = new RefreshAccountHandler(refresh, new RefreshAccountValidator());
        const string token = " \topaque-TokEn_+/=æ\r\n ";

        // Only presence is validated here; token interpretation belongs to Infrastructure.
        var result = await handler.Handle(new RefreshAccountCommand(token), cancellation.Token);

        Assert.Same(refresh.Result, result);
        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
        Assert.Equal(token, refresh.Token);
        Assert.Equal(cancellation.Token, refresh.CancellationToken);
        Assert.Equal(1, refresh.Calls);
    }

    [Fact]
    public async Task Invalid_refresh_token_failure_is_returned_unchanged()
    {
        var refresh = new RecordingRefresh
        {
            Result = AccountRefreshResult.Failure(
                new AccountRefreshError(AccountRefreshErrorCode.InvalidRefreshToken))
        };
        var handler = new RefreshAccountHandler(refresh, new RefreshAccountValidator());

        var result = await handler.Handle(new RefreshAccountCommand("invalid-token"), default);

        Assert.Same(refresh.Result, result);
        Assert.False(result.Succeeded);
        Assert.Equal(AccountRefreshErrorCode.InvalidRefreshToken, Assert.Single(result.Errors).Code);
        Assert.Equal(1, refresh.Calls);
    }

    private sealed class RecordingRefresh : IAccountRefresh
    {
        public AccountRefreshResult Result { get; init; } = AccountRefreshResult.Success();
        public int Calls { get; private set; }
        public string? Token { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<AccountRefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            Calls++;
            Token = refreshToken;
            CancellationToken = cancellationToken;
            return Task.FromResult(Result);
        }
    }
}
