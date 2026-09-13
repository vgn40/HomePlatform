namespace HomePlatform.Application.Accounts.Refresh;

public sealed record RefreshAccountCommand(
    string RefreshToken);