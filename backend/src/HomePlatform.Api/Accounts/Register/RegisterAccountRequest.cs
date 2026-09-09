namespace HomePlatform.Api.Accounts.Register;

public sealed record RegisterAccountRequest(
    string Email,
    string Password);
