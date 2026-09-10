using HomePlatform.Application.Accounts.Register;

namespace HomePlatform.Application.Tests.Accounts;

public sealed class RegisterAccountHandlerTests
{
    [Fact]
    public async Task Invalid_input_returns_typed_errors_without_calling_registration()
    {
        var registration = new RecordingRegistration();
        var handler = new RegisterAccountHandler(registration, new RegisterAccountValidator());

        var result = await handler.Handle(new RegisterAccountCommand(" ", " "), default);

        Assert.False(result.Succeeded);
        Assert.Null(result.AccountId);
        Assert.Equal(
            new[] { AccountRegistrationErrorCode.EmailRequired, AccountRegistrationErrorCode.PasswordRequired },
            result.Errors.Select(error => error.Code));
        Assert.Equal(0, registration.Calls);
    }

    [Fact]
    public async Task Valid_input_trims_email_and_forwards_password_token_and_result()
    {
        using var cancellation = new CancellationTokenSource();
        var registration = new RecordingRegistration();
        var handler = new RegisterAccountHandler(registration, new RegisterAccountValidator());

        // Password strength is owned by Identity; Application must forward it unchanged.
        var result = await handler.Handle(
            new RegisterAccountCommand("  person@example.com  ", " a "), cancellation.Token);

        Assert.Same(registration.Result, result);
        Assert.Equal(1, registration.Calls);
        Assert.Equal("person@example.com", registration.Email);
        Assert.Equal(" a ", registration.Password);
        Assert.Equal(cancellation.Token, registration.CancellationToken);
    }

    [Fact]
    public async Task Infrastructure_failure_is_returned_unchanged()
    {
        var registration = new RecordingRegistration
        {
            Result = AccountRegistrationResult.Failure(
                new AccountRegistrationError(AccountRegistrationErrorCode.EmailAlreadyExists))
        };
        var handler = new RegisterAccountHandler(registration, new RegisterAccountValidator());

        var result = await handler.Handle(
            new RegisterAccountCommand("person@example.com", "ValidPassword123!"), default);

        Assert.Same(registration.Result, result);
    }

    private sealed class RecordingRegistration : IAccountRegistration
    {
        public AccountRegistrationResult Result { get; init; } =
            AccountRegistrationResult.Success(Guid.NewGuid());
        public int Calls { get; private set; }
        public string? Email { get; private set; }
        public string? Password { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<AccountRegistrationResult> RegisterAsync(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            Calls++;
            Email = email;
            Password = password;
            CancellationToken = cancellationToken;
            return Task.FromResult(Result);
        }
    }
}
