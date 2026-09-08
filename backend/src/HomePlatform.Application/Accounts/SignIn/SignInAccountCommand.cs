namespace HomePlatform.Application.Accounts.SignIn;

public sealed record SignInAccountCommand(
    string Email,
    string Password);