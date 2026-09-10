using System.Net.Mail;

namespace HomePlatform.Application.Accounts.Register;

public sealed class RegisterAccountValidator
{
    private const int MaxEmailLength = 254;

    public IReadOnlyCollection<AccountRegistrationError> Validate(
        RegisterAccountCommand command)
    {
        var errors = new List<AccountRegistrationError>();

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            errors.Add(
                new AccountRegistrationError(
                    AccountRegistrationErrorCode.EmailRequired));
        }
        else
        {
            var email = command.Email.Trim();

            if (email.Length > MaxEmailLength)
            {
                errors.Add(
                    new AccountRegistrationError(
                        AccountRegistrationErrorCode.EmailTooLong));
            }
            else if (!MailAddress.TryCreate(email, out var mailAddress)
                     || !string.Equals(
                         mailAddress.Address,
                         email,
                         StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(
                    new AccountRegistrationError(
                        AccountRegistrationErrorCode.EmailInvalid));
            }
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            errors.Add(
                new AccountRegistrationError(
                    AccountRegistrationErrorCode.PasswordRequired));
        }

        return errors;
    }
}
