using HomePlatform.Application.Households.AddHouseholdMemberWithoutAccount;
using HomePlatform.Application.Households.CloseHousehold;
using HomePlatform.Application.Households.CreateHousehold;
using HomePlatform.Application.Households.LeaveHousehold;
using HomePlatform.Application.Households.TransferOwnership;

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

        endpoints.MapPost(
                "/api/households/{householdId:guid}/members/without-account",
                async (
                    Guid householdId,
                    AddHouseholdMemberWithoutAccountRequest request,
                    AddHouseholdMemberWithoutAccountHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var command =
                        new AddHouseholdMemberWithoutAccountCommand(
                            householdId,
                            request.DisplayName,
                            request.Role);

                    var result = await handler.Handle(
                        command,
                        cancellationToken);

                    return result.Outcome switch
                    {
                        AddHouseholdMemberWithoutAccountOutcome.Success
                            when result.PersonId is Guid personId
                            && result.MembershipId is Guid membershipId
                            => Results.Created(
                                $"/api/households/{householdId}/members/{membershipId}",
                                new AddHouseholdMemberWithoutAccountResponse(
                                    personId,
                                    membershipId)),

                        AddHouseholdMemberWithoutAccountOutcome.Unauthenticated
                            => Results.Unauthorized(),

                        AddHouseholdMemberWithoutAccountOutcome.NotFound
                            => Results.NotFound(),

                        AddHouseholdMemberWithoutAccountOutcome.Forbidden
                            => Results.Forbid(),

                        AddHouseholdMemberWithoutAccountOutcome.Invalid
                            => Results.Problem(
                                title: "Cannot add household member without account",
                                detail: result.Error,
                                statusCode: StatusCodes.Status400BadRequest),

                        _ => throw new InvalidOperationException(
                            "Unexpected AddHouseholdMemberWithoutAccount outcome.")
                    };
                })
            .RequireAuthorization()
            .WithName("AddHouseholdMemberWithoutAccount")
            .WithTags("Households")
            .Produces<AddHouseholdMemberWithoutAccountResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status403Forbidden)
            .Produces(
                StatusCodes.Status404NotFound);

        endpoints.MapPut(
                "/api/households/{householdId:guid}/ownership",
                async (
                    Guid householdId,
                    TransferOwnershipRequest request,
                    TransferOwnershipHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var command = new TransferOwnershipCommand(
                        householdId,
                        request.NewOwnerMembershipId);

                    var result = await handler.Handle(
                        command,
                        cancellationToken);

                    return result.Outcome switch
                    {
                        TransferOwnershipOutcome.Success
                            => Results.NoContent(),

                        TransferOwnershipOutcome.Unauthenticated
                            => Results.Unauthorized(),

                        TransferOwnershipOutcome.NotFound
                            => Results.NotFound(),

                        TransferOwnershipOutcome.Forbidden
                            => Results.Forbid(),

                        TransferOwnershipOutcome.Invalid
                            => Results.Problem(
                                title: "Invalid ownership transfer",
                                detail: result.ErrorMessage,
                                statusCode: StatusCodes.Status400BadRequest),

                        _ => throw new InvalidOperationException(
                            "Unexpected TransferOwnership outcome.")
                    };
                })
            .RequireAuthorization()
            .WithName("TransferOwnership")
            .WithTags("Households")
            .Produces(
                StatusCodes.Status204NoContent)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status403Forbidden)
            .Produces(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status400BadRequest);

        endpoints.MapDelete(
                "/api/households/{householdId:guid}/membership",
                async (
                    Guid householdId,
                    LeaveHouseholdHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var command = new LeaveHouseholdCommand(
                        householdId);

                    var result = await handler.Handle(
                        command,
                        cancellationToken);

                    return result.Outcome switch
                    {
                        LeaveHouseholdOutcome.Success
                            => Results.NoContent(),

                        LeaveHouseholdOutcome.Unauthenticated
                            => Results.Unauthorized(),

                        LeaveHouseholdOutcome.NotFound
                            => Results.NotFound(),

                        LeaveHouseholdOutcome.Forbidden
                            => Results.Forbid(),

                        LeaveHouseholdOutcome.Invalid
                            => Results.Problem(
                                title: "Cannot leave household",
                                detail: result.ErrorMessage,
                                statusCode: StatusCodes.Status409Conflict),

                        _ => throw new InvalidOperationException(
                            "Unexpected LeaveHousehold outcome.")
                    };
                })
            .RequireAuthorization()
            .WithName("LeaveHousehold")
            .WithTags("Households")
            .Produces(
                StatusCodes.Status204NoContent)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status403Forbidden)
            .Produces(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        endpoints.MapDelete(
                "/api/households/{householdId:guid}",
                async (
                    Guid householdId,
                    CloseHouseholdHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    var command = new CloseHouseholdCommand(
                        householdId);

                    var result = await handler.Handle(
                        command,
                        cancellationToken);

                    return result.Outcome switch
                    {
                        CloseHouseholdOutcome.Success
                            => Results.NoContent(),

                        CloseHouseholdOutcome.Unauthenticated
                            => Results.Unauthorized(),

                        CloseHouseholdOutcome.NotFound
                            => Results.NotFound(),

                        CloseHouseholdOutcome.Forbidden
                            => Results.Forbid(),

                        _ => throw new InvalidOperationException(
                            "Unexpected CloseHousehold outcome.")
                    };
                })
            .RequireAuthorization()
            .WithName("CloseHousehold")
            .WithTags("Households")
            .Produces(
                StatusCodes.Status204NoContent)
            .Produces(
                StatusCodes.Status401Unauthorized)
            .Produces(
                StatusCodes.Status403Forbidden)
            .Produces(
                StatusCodes.Status404NotFound);

        return endpoints;
    }
}