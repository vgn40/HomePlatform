using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HomePlatform.IntegrationTests;

internal sealed class TestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string AccountIdHeaderName = "X-Test-Account-Id";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(
                AccountIdHeaderName,
                out var accountIdHeader))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        if (!Guid.TryParse(accountIdHeader.ToString(), out var accountId) ||
            accountId == Guid.Empty)
        {
            return Task.FromResult(
                AuthenticateResult.Fail(
                    $"{AccountIdHeaderName} must contain a non-empty GUID."));
        }

        var claims = new[]
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                accountId.ToString())
        };

        var identity = new ClaimsIdentity(
            claims,
            SchemeName);

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(
            principal,
            SchemeName);

        return Task.FromResult(
            AuthenticateResult.Success(ticket));
    }
}
