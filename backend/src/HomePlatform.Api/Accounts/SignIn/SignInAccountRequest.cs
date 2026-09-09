namespace HomePlatform.Api.Accounts.SignIn;

public sealed record SignInAccountRequest(
    string Email,
    string Password);
