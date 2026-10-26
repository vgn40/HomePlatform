using HomePlatform.Application.Households;
using HomePlatform.Application.Households.GetHousehold;
using HomePlatform.Application.Households.ListHouseholds;
using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Tests.Households;

public sealed class HouseholdReadHandlerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_current_person_returns_unauthenticated_without_reading(bool detail)
    {
        var store = new RecordingReadStore();
        var currentPerson = new CurrentPerson(null);

        if (detail)
        {
            var result = await new GetHouseholdHandler(store, currentPerson)
                .Handle(new GetHouseholdQuery(Guid.NewGuid()));
            Assert.Equal(GetHouseholdOutcome.Unauthenticated, result.Outcome);
        }
        else
        {
            var result = await new ListHouseholdsHandler(store, currentPerson).Handle();
            Assert.Equal(ListHouseholdsOutcome.Unauthenticated, result.Outcome);
        }

        Assert.Equal(0, store.CallCount);
    }

    [Fact]
    public async Task List_passes_resolved_person_and_cancellation_and_returns_summaries()
    {
        using var cancellation = new CancellationTokenSource();
        var currentPerson = new CurrentPerson(Guid.NewGuid());
        var store = new RecordingReadStore
        {
            Summaries = [new HouseholdSummaryResponse(Guid.NewGuid(), "Home", HouseholdRole.Guest, 3)]
        };

        var result = await new ListHouseholdsHandler(store, currentPerson).Handle(cancellation.Token);

        Assert.Equal(ListHouseholdsOutcome.Success, result.Outcome);
        Assert.Same(store.Summaries, result.Households);
        Assert.Equal(currentPerson.PersonId, store.PersonId);
        Assert.Equal(cancellation.Token, currentPerson.CancellationToken);
        Assert.Equal(cancellation.Token, store.CancellationToken);
        Assert.Equal(1, store.CallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Detail_passes_resolved_person_and_maps_missing_or_visible_household(bool visible)
    {
        using var cancellation = new CancellationTokenSource();
        var householdId = Guid.NewGuid();
        var currentPerson = new CurrentPerson(Guid.NewGuid());
        var store = new RecordingReadStore
        {
            Detail = visible
                ? new HouseholdDetailResponse(householdId, "Home", DateTime.UtcNow,
                    DateTime.UtcNow, HouseholdRole.Member, [])
                : null
        };

        var result = await new GetHouseholdHandler(store, currentPerson)
            .Handle(new GetHouseholdQuery(householdId), cancellation.Token);

        Assert.Equal(visible ? GetHouseholdOutcome.Success : GetHouseholdOutcome.NotFound, result.Outcome);
        Assert.Same(store.Detail, result.Household);
        Assert.Equal(householdId, store.HouseholdId);
        Assert.Equal(currentPerson.PersonId, store.PersonId);
        Assert.Equal(cancellation.Token, currentPerson.CancellationToken);
        Assert.Equal(cancellation.Token, store.CancellationToken);
        Assert.Equal(1, store.CallCount);
    }

    private sealed class CurrentPerson(Guid? personId) : ICurrentPerson
    {
        public Guid? PersonId => personId;
        public CancellationToken CancellationToken { get; private set; }

        public Task<Guid?> GetPersonIdAsync(CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            return Task.FromResult(personId);
        }
    }

    private sealed class RecordingReadStore : IHouseholdQueryService
    {
        public IReadOnlyList<HouseholdSummaryResponse> Summaries { get; init; } = [];
        public HouseholdDetailResponse? Detail { get; init; }
        public Guid PersonId { get; private set; }
        public Guid HouseholdId { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public int CallCount { get; private set; }

        public Task<IReadOnlyList<HouseholdSummaryResponse>> ListForPersonAsync(
            Guid personId, CancellationToken cancellationToken = default)
        {
            PersonId = personId;
            CancellationToken = cancellationToken;
            CallCount++;
            return Task.FromResult(Summaries);
        }

        public Task<HouseholdDetailResponse?> GetForPersonAsync(
            Guid householdId, Guid personId, CancellationToken cancellationToken = default)
        {
            HouseholdId = householdId;
            PersonId = personId;
            CancellationToken = cancellationToken;
            CallCount++;
            return Task.FromResult(Detail);
        }
    }
}
