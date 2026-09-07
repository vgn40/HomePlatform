namespace HomePlatform.Application.Accounts.RegisterAccount;

public sealed record RegisterAccountCommand(
    string Email,
    string Password);
