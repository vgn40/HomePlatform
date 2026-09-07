namespace HomePlatform.Api.Accounts;

public sealed record RegisterAccountRequest(
    string Email,
    string Password);
