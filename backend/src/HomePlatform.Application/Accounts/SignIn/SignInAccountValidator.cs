namespace HomePlatform.Application.Accounts.SignIn;

public sealed class SignInAccountValidator
{
    public IReadOnlyCollection<AccountAuthenticationError> Validate(
        SignInAccountCommand command)
    {
        var errors = new List<AccountAuthenticationError>();

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            errors.Add(
                new AccountAuthenticationError(
                    AccountAuthenticationErrorCode.EmailRequired));
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            errors.Add(
                new AccountAuthenticationError(
                    AccountAuthenticationErrorCode.PasswordRequired));
        }

        return errors;
    }
}