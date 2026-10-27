using HomePlatform.Application.Households;
using HomePlatform.Application.Households.RemoveHouseholdMember;
using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Tests.Households.RemoveHouseholdMember;

public sealed class RemoveHouseholdMemberHandlerTests
{
    [Theory]
    [InlineData("anonymous", RemoveHouseholdMemberOutcome.Unauthenticated, 0)]
    [InlineData("missing-household", RemoveHouseholdMemberOutcome.NotFound, 1)]
    [InlineData("outsider", RemoveHouseholdMemberOutcome.NotFound, 1)]
    [InlineData("member", RemoveHouseholdMemberOutcome.Forbidden, 1)]
    [InlineData("guest", RemoveHouseholdMemberOutcome.Forbidden, 1)]
    [InlineData("missing-membership", RemoveHouseholdMemberOutcome.NotFound, 1)]
    [InlineData("owner-target", RemoveHouseholdMemberOutcome.Invalid, 1)]
    [InlineData("self", RemoveHouseholdMemberOutcome.Invalid, 1)]
    public async Task Expected_failures_do_not_persist(string scenario, RemoveHouseholdMemberOutcome expected, int loads)
    {
        var owner = Guid.NewGuid();
        var member = Guid.NewGuid();
        var household = new Household("Home", owner);
        Assert.True(household.AddMember(owner, scenario == "guest" ? HouseholdRole.Guest : HouseholdRole.Member, member).IsSuccess);
        Assert.True(household.AddMember(owner, HouseholdRole.Owner, Guid.NewGuid()).IsSuccess);
        var actor = scenario switch { "anonymous" => (Guid?)null, "outsider" => Guid.NewGuid(),
            "member" or "guest" => member, _ => owner };
        var target = scenario switch
        {
            "self" => household.Members.Single(m => m.PersonId == owner).MembershipId,
            "owner-target" => household.Members.Single(m => m.Role == HouseholdRole.Owner && m.PersonId != owner).MembershipId,
            "missing-membership" => Guid.NewGuid(),
            _ => household.Members.Single(m => m.PersonId == member).MembershipId
        };
        var before = household.Members.ToArray();
        var timestamp = household.UpdatedAt;
        var repository = new RecordingRepository(scenario == "missing-household" ? null : household);
        var handler = new RemoveHouseholdMemberHandler(repository, new CurrentPerson(actor));

        var result = await handler.Handle(new RemoveHouseholdMemberCommand(household.Id, target));

        Assert.Equal(expected, result.Outcome);
        Assert.Equal(loads, repository.Loads);
        Assert.Equal(0, repository.Saves);
        Assert.Equal(before, household.Members);
        Assert.Equal(timestamp, household.UpdatedAt);
        Assert.Equal(expected == RemoveHouseholdMemberOutcome.Invalid, result.ErrorMessage is not null);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Success_persists_same_aggregate_once_and_forwards_cancellation(HouseholdRole role)
    {
        var owner = Guid.NewGuid();
        var household = new Household("Home", owner);
        Assert.True(household.AddMember(owner, role, Guid.NewGuid()).IsSuccess);
        var target = household.Members.Single(m => m.Role == role);
        var repository = new RecordingRepository(household);
        var current = new CurrentPerson(owner);
        using var cancellation = new CancellationTokenSource();
        var result = await new RemoveHouseholdMemberHandler(repository, current).Handle(
            new RemoveHouseholdMemberCommand(household.Id, target.MembershipId), cancellation.Token);

        Assert.Equal(RemoveHouseholdMemberOutcome.Success, result.Outcome);
        Assert.Equal(new[] { "get", "update" }, repository.Calls);
        Assert.Equal(1, repository.Loads);
        Assert.Equal(1, repository.Saves);
        Assert.Equal(household.Id, repository.RequestedId);
        Assert.Same(household, repository.Saved);
        Assert.DoesNotContain(household.Members, m => m.MembershipId == target.MembershipId);
        Assert.Equal(cancellation.Token, current.Token);
        Assert.Equal(cancellation.Token, repository.LoadToken);
        Assert.Equal(cancellation.Token, repository.SaveToken);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Infrastructure_failure_is_not_reported_as_business_error(bool concurrency)
    {
        var owner = Guid.NewGuid();
        var household = new Household("Home", owner);
        Assert.True(household.AddMember(owner, HouseholdRole.Member, Guid.NewGuid()).IsSuccess);
        Exception failure = concurrency ? new HouseholdConcurrencyException(new Exception("race"))
            : new InvalidOperationException("database failed");
        var repository = new RecordingRepository(household) { Failure = failure };
        var handler = new RemoveHouseholdMemberHandler(repository, new CurrentPerson(owner));
        Assert.Same(failure, await Record.ExceptionAsync(() => handler.Handle(new RemoveHouseholdMemberCommand(
            household.Id, household.Members.Single(m => m.Role == HouseholdRole.Member).MembershipId))));
    }

    [Fact]
    public async Task Null_command_does_not_resolve_or_load()
    {
        var repository = new RecordingRepository(null);
        var current = new CurrentPerson(null);
        await Assert.ThrowsAsync<ArgumentNullException>(() => new RemoveHouseholdMemberHandler(repository, current).Handle(null!));
        Assert.Equal(0, current.Calls);
        Assert.Equal(0, repository.Loads);
    }

    private sealed class CurrentPerson(Guid? id) : ICurrentPerson
    {
        public int Calls { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task<Guid?> GetPersonIdAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            Token = cancellationToken;
            return Task.FromResult(id);
        }
    }

    private sealed class RecordingRepository(Household? household) : IHouseholdRepository
    {
        public List<string> Calls { get; } = new();
        public int Loads { get; private set; }
        public int Saves { get; private set; }
        public Guid RequestedId { get; private set; }
        public Household? Saved { get; private set; }
        public CancellationToken LoadToken { get; private set; }
        public CancellationToken SaveToken { get; private set; }
        public Exception? Failure { get; init; }
        public Task<Household?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            Calls.Add("get"); Loads++; RequestedId = id; LoadToken = cancellationToken;
            return Task.FromResult(household);
        }
        public Task UpdateAsync(Household value, CancellationToken cancellationToken = default)
        {
            Calls.Add("update"); Saves++; Saved = value; SaveToken = cancellationToken;
            return Failure is null ? Task.CompletedTask : Task.FromException(Failure);
        }
        public Task AddAsync(Household value, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Unexpected add");
        public Task DeleteAsync(Household value, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Unexpected household deletion");
    }
}
