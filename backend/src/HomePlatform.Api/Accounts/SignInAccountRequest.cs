namespace HomePlatform.Api.Accounts;

public sealed record SignInAccountRequest(
    string Email,
    string Password);