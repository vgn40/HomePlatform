using HomePlatform.Application.Households;
using HomePlatform.Application.Households.AddHouseholdMemberWithoutAccount;
using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;
using HomePlatform.Domain.Person;

namespace HomePlatform.Application.Tests.Households.AddHouseholdMemberWithoutAccount;

public sealed class AddHouseholdMemberWithoutAccountHandlerTests
{
    [Theory]
    [InlineData("anonymous", AddHouseholdMemberWithoutAccountOutcome.Unauthenticated)]
    [InlineData("missing", AddHouseholdMemberWithoutAccountOutcome.NotFound)]
    [InlineData("outsider", AddHouseholdMemberWithoutAccountOutcome.NotFound)]
    [InlineData("member", AddHouseholdMemberWithoutAccountOutcome.Forbidden)]
    [InlineData("guest", AddHouseholdMemberWithoutAccountOutcome.Forbidden)]
    [InlineData("empty", AddHouseholdMemberWithoutAccountOutcome.Invalid)]
    [InlineData("blank", AddHouseholdMemberWithoutAccountOutcome.Invalid)]
    [InlineData("invalid-role", AddHouseholdMemberWithoutAccountOutcome.Invalid)]
    public async Task Handle_failure_does_not_persist_or_mutate_household(
        string scenario, AddHouseholdMemberWithoutAccountOutcome expected)
    {
        var ownerId = Guid.NewGuid();
        var household = new Household("Home", ownerId);
        Guid? actorId = ownerId;
        if (scenario is "member" or "guest")
        {
            actorId = Guid.NewGuid();
            Assert.True(household.AddMember(ownerId,
                scenario == "member" ? HouseholdRole.Member : HouseholdRole.Guest,
                actorId.Value).IsSuccess);
        }
        if (scenario == "anonymous") actorId = null;
        if (scenario == "outsider") actorId = Guid.NewGuid();
        var before = household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray();
        var updatedAt = household.UpdatedAt;
        var repository = new RecordingHouseholdRepository(scenario == "missing" ? null : household);
        var persistence = new RecordingPersistence();
        var handler = new AddHouseholdMemberWithoutAccountHandler(repository,
            new FakeCurrentPerson(actorId), persistence);

        var result = await handler.Handle(new(household.Id,
            scenario == "empty" ? "" : scenario == "blank" ? "   " : "Alma",
            scenario == "invalid-role" ? (HouseholdRole)999 : HouseholdRole.Member));

        Assert.Equal(expected, result.Outcome);
        Assert.Null(result.PersonId);
        Assert.Null(result.MembershipId);
        Assert.Equal(0, persistence.CallCount);
        Assert.Equal(scenario == "anonymous" ? 0 : 1, repository.GetCallCount);
        Assert.Equal(before, household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray());
        Assert.Equal(updatedAt, household.UpdatedAt);
    }

    [Fact]
    public async Task Handle_owner_adding_accountless_owner_returns_invalid_without_persisting_or_mutating_household()
    {
        var ownerId = Guid.NewGuid();
        var household = new Household("Home", ownerId);
        var actorId = ownerId;
        var before = household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray();
        var updatedAt = household.UpdatedAt;
        var repository = new RecordingHouseholdRepository(household);
        var persistence = new RecordingPersistence();
        var handler = new AddHouseholdMemberWithoutAccountHandler(repository,
            new FakeCurrentPerson(actorId), persistence);

        var result = await handler.Handle(new(household.Id, "Alma", HouseholdRole.Owner));

        Assert.Equal(AddHouseholdMemberWithoutAccountOutcome.Invalid, result.Outcome);
        Assert.Equal("A person without an account cannot be an owner.", result.Error);
        Assert.Null(result.PersonId);
        Assert.Null(result.MembershipId);
        Assert.Equal(0, persistence.CallCount);
        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(before, household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray());
        Assert.Equal(updatedAt, household.UpdatedAt);
    }

    [Fact]
    public async Task Handle_outsider_with_owner_role_returns_not_found_without_persisting_or_mutating_household()
    {
        var ownerId = Guid.NewGuid();
        var household = new Household("Home", ownerId);
        var actorId = Guid.NewGuid();
        var before = household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray();
        var updatedAt = household.UpdatedAt;
        var repository = new RecordingHouseholdRepository(household);
        var persistence = new RecordingPersistence();
        var handler = new AddHouseholdMemberWithoutAccountHandler(repository,
            new FakeCurrentPerson(actorId), persistence);

        var result = await handler.Handle(new(household.Id, "Alma", HouseholdRole.Owner));

        Assert.Equal(AddHouseholdMemberWithoutAccountOutcome.NotFound, result.Outcome);
        Assert.Null(result.PersonId);
        Assert.Null(result.MembershipId);
        Assert.Equal(0, persistence.CallCount);
        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(before, household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray());
        Assert.Equal(updatedAt, household.UpdatedAt);
    }

    [Fact]
    public async Task Handle_outsider_with_invalid_display_name_returns_not_found_without_persisting_or_mutating_household()
    {
        var ownerId = Guid.NewGuid();
        var household = new Household("Home", ownerId);
        var actorId = Guid.NewGuid();
        var before = household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray();
        var updatedAt = household.UpdatedAt;
        var repository = new RecordingHouseholdRepository(household);
        var persistence = new RecordingPersistence();
        var handler = new AddHouseholdMemberWithoutAccountHandler(repository,
            new FakeCurrentPerson(actorId), persistence);

        var result = await handler.Handle(new(household.Id, "   ", HouseholdRole.Member));

        Assert.Equal(AddHouseholdMemberWithoutAccountOutcome.NotFound, result.Outcome);
        Assert.Null(result.PersonId);
        Assert.Null(result.MembershipId);
        Assert.Equal(0, persistence.CallCount);
        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(before, household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray());
        Assert.Equal(updatedAt, household.UpdatedAt);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Handle_non_owner_with_invalid_display_name_returns_forbidden_without_persisting_or_mutating_household(HouseholdRole currentRole)
    {
        var ownerId = Guid.NewGuid();
        var household = new Household("Home", ownerId);
        var actorId = Guid.NewGuid();
        Assert.True(household.AddMember(ownerId, currentRole, actorId).IsSuccess);
        var before = household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray();
        var updatedAt = household.UpdatedAt;
        var repository = new RecordingHouseholdRepository(household);
        var persistence = new RecordingPersistence();
        var handler = new AddHouseholdMemberWithoutAccountHandler(repository,
            new FakeCurrentPerson(actorId), persistence);

        var result = await handler.Handle(new(household.Id, "   ", HouseholdRole.Member));

        Assert.Equal(AddHouseholdMemberWithoutAccountOutcome.Forbidden, result.Outcome);
        Assert.Null(result.PersonId);
        Assert.Null(result.MembershipId);
        Assert.Equal(0, persistence.CallCount);
        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(before, household.Members.Select(m => (m.MembershipId, m.PersonId, m.Role)).ToArray());
        Assert.Equal(updatedAt, household.UpdatedAt);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Handle_owner_creates_person_and_membership_and_persists_once(HouseholdRole role)
    {
        var ownerId = Guid.NewGuid();
        var household = new Household("Home", ownerId);
        var repository = new RecordingHouseholdRepository(household);
        var persistence = new RecordingPersistence();
        var currentPerson = new FakeCurrentPerson(ownerId);
        var handler = new AddHouseholdMemberWithoutAccountHandler(repository, currentPerson, persistence);
        using var cancellation = new CancellationTokenSource();

        var result = await handler.Handle(new(household.Id, "  Alma  ", role), cancellation.Token);

        Assert.Equal(AddHouseholdMemberWithoutAccountOutcome.Success, result.Outcome);
        Assert.Null(result.Error);
        Assert.Equal(1, persistence.CallCount);
        var person = Assert.IsType<Person>(persistence.Person);
        Assert.NotEqual(Guid.Empty, person.Id);
        Assert.NotEqual(ownerId, person.Id);
        Assert.Equal("Alma", person.DisplayName);
        Assert.Equal(person.Id, result.PersonId);
        Assert.Same(household, persistence.Household);
        var member = Assert.Single(household.Members, m => m.PersonId == person.Id);
        Assert.Equal(role, member.Role);
        Assert.NotEqual(Guid.Empty, member.MembershipId);
        Assert.Equal(member.MembershipId, result.MembershipId);
        Assert.Equal(2, household.Members.Count);
        Assert.Equal(household.Id, repository.RequestedId);
        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(cancellation.Token, currentPerson.Token);
        Assert.Equal(cancellation.Token, repository.Token);
        Assert.Equal(cancellation.Token, persistence.Token);
        // Only Person, Household and current-person boundaries are needed: no account service.
    }

    private sealed class FakeCurrentPerson(Guid? personId) : ICurrentPerson
    {
        public CancellationToken Token { get; private set; }
        public Task<Guid?> GetPersonIdAsync(CancellationToken cancellationToken = default)
        {
            Token = cancellationToken;
            return Task.FromResult(personId);
        }
    }

    private sealed class RecordingPersistence : IAddHouseholdMemberWithoutAccountPersistence
    {
        public int CallCount { get; private set; }
        public Person? Person { get; private set; }
        public Household? Household { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task SaveAsync(Person person, Household household, CancellationToken cancellationToken = default)
        {
            CallCount++;
            Person = person;
            Household = household;
            Token = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingHouseholdRepository(Household? household) : IHouseholdRepository
    {
        public int GetCallCount { get; private set; }
        public Guid RequestedId { get; private set; }
        public CancellationToken Token { get; private set; }
        public Task<Household?> GetByIdAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            RequestedId = householdId;
            Token = cancellationToken;
            return Task.FromResult(household?.Id == householdId ? household : null);
        }
        public Task AddAsync(Household household, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Repository write was not expected.");
        public Task UpdateAsync(Household household, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Repository write was not expected.");
        public Task DeleteAsync(Household household, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Repository write was not expected.");
    }
}
