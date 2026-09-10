using HomePlatform.Application.Accounts.SignIn;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Mvc;

namespace HomePlatform.Api.Accounts.SignIn;

public static class SignInAccountEndpoint
{
    public static IEndpointRouteBuilder MapSignInAccountEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/sign-in",
            Handle)
            .AllowAnonymous()
            .WithName("SignInAccount")
            .Produces<AccessTokenResponse>(
                StatusCodes.Status200OK)
            .Produces<ProblemDetails>(
                StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(
                StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> Handle(
        SignInAccountRequest request,
        SignInAccountHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new SignInAccountCommand(
            request.Email,
            request.Password);

        var result = await handler.Handle(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            var validationFailure = result.Errors.Any(
                error =>
                    error.Code is
                        AccountAuthenticationErrorCode.EmailRequired
                        or AccountAuthenticationErrorCode.PasswordRequired);

            if (validationFailure)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Sign-in request is invalid.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["errors"] = result.Errors
                            .Select(error => error.Code.ToString())
                            .ToArray()
                    });
            }

            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Sign-in failed.",
                extensions: new Dictionary<string, object?>
                {
                    ["errors"] = new[]
                    {
                        AccountAuthenticationErrorCode
                            .InvalidCredentials
                            .ToString()
                    }
                });
        }

        return Results.Empty;
    }
}
