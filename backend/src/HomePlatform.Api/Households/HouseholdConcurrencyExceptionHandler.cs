
using HomePlatform.Application.Households;
using Microsoft.AspNetCore.Diagnostics;

namespace HomePlatform.Api.Households;

public sealed class HouseholdConcurrencyExceptionHandler
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not HouseholdConcurrencyException)
        {
            return false;
        }

        await Results.Problem(
            title: "Household concurrency conflict",
            detail: "The household was modified by another request. Reload and try again.",
            statusCode: StatusCodes.Status409Conflict)
            .ExecuteAsync(httpContext);

        return true;
    }
}
