using HomePlatform.Application.Identity;

namespace HomePlatform.Application.Households.ListHouseholds;

public sealed class ListHouseholdsHandler(
    IHouseholdQueryService queryService,
    ICurrentPerson currentPerson)
{
    public async Task<ListHouseholdsResult> Handle(
        CancellationToken cancellationToken = default)
    {
        var personId = await currentPerson.GetPersonIdAsync(cancellationToken);
        if (personId is null)
        {
            return ListHouseholdsResult.Unauthenticated();
        }

        var households = await queryService.ListForPersonAsync(
            personId.Value,
            cancellationToken);

        return ListHouseholdsResult.Success(households);
    }
}
