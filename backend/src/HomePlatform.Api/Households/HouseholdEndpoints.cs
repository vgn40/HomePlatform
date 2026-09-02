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
                    var command = new CreateHouseholdCommand(
                        request.Name);

                    var result = await handler.Handle(
                        command,
                        cancellationToken);

                    return result.Outcome switch
                    {
                        CreateHouseholdOutcome.Success
                            when result.HouseholdId is Guid householdId
                            && result.Name is not null
                            => Results.Created(
                                $"/api/households/{householdId}",
                                new CreateHouseholdResponse(
                                    householdId,
                                    result.Name)),

                        CreateHouseholdOutcome.Invalid
                            => Results.Problem(
                                title: "Invalid household",
                                detail: result.ErrorMessage,
                                statusCode: StatusCodes.Status400BadRequest),

                        CreateHouseholdOutcome.Unauthenticated
                            => Results.Unauthorized(),

                        _ => throw new InvalidOperationException(
                            "Unexpected CreateHousehold outcome.")
                    };
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
