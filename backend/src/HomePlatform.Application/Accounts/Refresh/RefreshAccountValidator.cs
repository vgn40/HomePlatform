namespace HomePlatform.Application.Accounts.Refresh;

public sealed class RefreshAccountValidator
{
    public IReadOnlyCollection<AccountRefreshError> Validate(
        RefreshAccountCommand command)
    {
        var errors = new List<AccountRefreshError>();

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            errors.Add(
                new AccountRefreshError(
                    AccountRefreshErrorCode.RefreshTokenRequired));
        }

        return errors;
    }
}