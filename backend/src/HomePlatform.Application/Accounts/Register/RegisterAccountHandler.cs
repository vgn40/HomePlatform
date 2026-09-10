namespace HomePlatform.Application.Accounts.Register;

public sealed class RegisterAccountHandler(
    IAccountRegistration accountRegistration,
    RegisterAccountValidator validator)
{
    public Task<AccountRegistrationResult> Handle(
        RegisterAccountCommand command,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(command);

        if (errors.Count > 0)
        {
            return Task.FromResult(
                AccountRegistrationResult.Failure(errors.ToArray()));
        }

        return accountRegistration.RegisterAsync(
            command.Email.Trim(),
            command.Password,
            cancellationToken);
    }
}
