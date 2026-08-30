using HomePlatform.Application.Households.CreateHousehold;

namespace HomePlatform.Api.Households;

public static class HouseholdEndpoints
{
    public static IEndpointRouteBuilder MapHouseholdEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/api/households",
                async (
                    CreateHouseholdRequest request,
                    CreateHouseholdHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    try
                    {
                        var command = new CreateHouseholdCommand(
                            request.Name);

                        var result = await handler.Handle(
                            command,
                            cancellationToken);

                        var response = new CreateHouseholdResponse(
                            result.HouseholdId,
                            result.Name);

                        return Results.Created(
                            $"/api/households/{result.HouseholdId}",
                            response);
                    }
                    catch (ArgumentException exception)
                    {
                        return Results.Problem(
                            title: "Invalid household",
                            detail: exception.Message,
                            statusCode: StatusCodes.Status400BadRequest);
                    }
                })
            .RequireAuthorization()
            .WithName("CreateHousehold")
            .WithTags("Households")
            .Produces<CreateHouseholdResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized);

        return endpoints;
    }
}