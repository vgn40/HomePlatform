using Microsoft.EntityFrameworkCore;
using HomePlatform.Infrastructure.Persistence;

namespace HomePlatform.Api;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("Health")
            .WithTags("Health");

        endpoints.MapGet(
                "/ready",
                async (HomePlatformDbContext dbContext, CancellationToken cancellationToken) =>
                    await dbContext.Database.CanConnectAsync(cancellationToken)
                        ? Results.Ok(new { status = "ready" })
                        : Results.Json(
                            new { status = "unavailable" },
                            statusCode: StatusCodes.Status503ServiceUnavailable))
            .WithName("Readiness")
            .WithTags("Health");

        return endpoints;
    }
}
