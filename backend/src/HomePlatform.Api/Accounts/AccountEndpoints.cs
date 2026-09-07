using HomePlatform.Application.Accounts.RegisterAccount;
using Microsoft.AspNetCore.Mvc;

namespace HomePlatform.Api.Accounts;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/accounts/register",
            async (
                RegisterAccountRequest request,
                RegisterAccountHandler handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RegisterAccountCommand(
                    request.Email,
                    request.Password);

                var result = await handler.Handle(
                    command,
                    cancellationToken);

                if (!result.Succeeded)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Account registration failed.",
                        extensions: new Dictionary<string, object?>
                        {
                            ["errors"] = result.Errors
                                .Select(error => error.Code.ToString())
                                .ToArray()
                        });
                }

                var accountId = result.AccountId
                    ?? throw new InvalidOperationException(
                        "Successful registration must contain an account id.");

                return Results.Created(
                    $"/api/accounts/{accountId}",
                    new RegisterAccountResponse(accountId));
            })
            .AllowAnonymous()
            .WithName("RegisterAccount")
            .WithTags("Accounts")
            .Produces<RegisterAccountResponse>(
                StatusCodes.Status201Created)
            .Produces<ProblemDetails>(
                StatusCodes.Status400BadRequest);

        return endpoints;
    }
}