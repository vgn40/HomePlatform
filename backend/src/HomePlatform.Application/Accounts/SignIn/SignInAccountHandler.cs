using HomePlatform.Application.Accounts;

namespace HomePlatform.Application.Accounts.SignIn;

public sealed class SignInAccountHandler(
    IAccountAuthentication accountAuthentication,
    SignInAccountValidator validator)
{
    public Task<AccountAuthenticationResult> Handle(
        SignInAccountCommand command,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(command);

        if (errors.Count > 0)
        {
            return Task.FromResult(
                AccountAuthenticationResult.Failure(
                    errors.ToArray()));
        }

        return accountAuthentication.AuthenticateAsync(
            command.Email.Trim(),
            command.Password,
            cancellationToken);
    }
}