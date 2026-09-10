namespace HomePlatform.Application.Accounts.Register;

public sealed record RegisterAccountCommand(
    string Email,
    string Password);
