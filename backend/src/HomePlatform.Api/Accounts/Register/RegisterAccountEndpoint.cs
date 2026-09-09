using HomePlatform.Application.Accounts.RegisterAccount;
using Microsoft.AspNetCore.Mvc;

namespace HomePlatform.Api.Accounts.Register;

public static class RegisterAccountEndpoint
{
    public static IEndpointRouteBuilder MapRegisterAccountEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/register",
            Handle)
            .AllowAnonymous()
            .WithName("RegisterAccount")
            .Produces<RegisterAccountResponse>(
                StatusCodes.Status201Created)
            .Produces<ProblemDetails>(
                StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> Handle(
        RegisterAccountRequest request,
        RegisterAccountHandler handler,
        CancellationToken cancellationToken)
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
    }
}
