using HomePlatform.Application.Accounts.Refresh;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Mvc;

namespace HomePlatform.Api.Accounts.Refresh;

public static class RefreshAccountEndpoint
{
    public static IEndpointRouteBuilder MapRefreshAccountEndpoint(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/refresh",
            Handle)
            .AllowAnonymous()
            .WithName("RefreshAccount")
            .Produces<AccessTokenResponse>(
                StatusCodes.Status200OK)
            .Produces<ProblemDetails>(
                StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(
                StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> Handle(
        RefreshAccountRequest request,
        RefreshAccountHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new RefreshAccountCommand(
            request.RefreshToken);

        var result = await handler.Handle(
            command,
            cancellationToken);

        if (!result.Succeeded)
        {
            var validationFailure = result.Errors.Any(
                error =>
                    error.Code ==
                    AccountRefreshErrorCode.RefreshTokenRequired);

            if (validationFailure)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Refresh request is invalid.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["errors"] = result.Errors
                            .Select(error => error.Code.ToString())
                            .ToArray()
                    });
            }

            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Refresh failed.",
                extensions: new Dictionary<string, object?>
                {
                    ["errors"] = new[]
                    {
                        AccountRefreshErrorCode
                            .InvalidRefreshToken
                            .ToString()
                    }
                });
        }

        return Results.Empty;
    }
}