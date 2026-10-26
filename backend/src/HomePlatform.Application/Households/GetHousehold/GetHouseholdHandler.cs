using HomePlatform.Application.Identity;

namespace HomePlatform.Application.Households.GetHousehold;

public sealed class GetHouseholdHandler(
    IHouseholdQueryService queryService,
    ICurrentPerson currentPerson)
{
    public async Task<GetHouseholdResult> Handle(
        GetHouseholdQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var personId = await currentPerson.GetPersonIdAsync(cancellationToken);
        if (personId is null)
        {
            return GetHouseholdResult.Unauthenticated();
        }

        var household = await queryService.GetForPersonAsync(
            query.HouseholdId,
            personId.Value,
            cancellationToken);

        return household is null
            ? GetHouseholdResult.NotFound()
            : GetHouseholdResult.Success(household);
    }
}
