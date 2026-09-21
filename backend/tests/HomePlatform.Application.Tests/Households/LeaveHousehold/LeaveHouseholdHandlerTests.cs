using HomePlatform.Application.Households;
using HomePlatform.Application.Households.LeaveHousehold;
using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Tests.Households.LeaveHousehold;

public sealed class LeaveHouseholdHandlerTests
{
    [Fact]
    public async Task Handle_unauthenticated_does_not_load_or_update()
    {
        var repository = new RecordingHouseholdRepository();

        var handler = new LeaveHouseholdHandler(
            repository,
            new FakeCurrentPerson(null));

        var result = await handler.Handle(
            new LeaveHouseholdCommand(Guid.NewGuid()));

        Assert.Equal(
            LeaveHouseholdOutcome.Unauthenticated,
            result.Outcome);

        Assert.Equal(0, repository.GetCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task Handle_missing_household_returns_not_found_without_update()
    {
        var repository = new RecordingHouseholdRepository();
        var id = Guid.NewGuid();

        var handler = new LeaveHouseholdHandler(
            repository,
            new FakeCurrentPerson(Guid.NewGuid()));

        var result = await handler.Handle(
            new LeaveHouseholdCommand(id));

        Assert.Equal(
            LeaveHouseholdOutcome.NotFound,
            result.Outcome);

        Assert.Equal(
            id,
            repository.RequestedHouseholdId);

        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task Handle_non_member_returns_not_found_without_update()
    {
        var household = new Household(
            "Home",
            Guid.NewGuid());

        Assert.True(
            household.AddMember(
                HouseholdRole.Member,
                Guid.NewGuid()).IsSuccess);

        var before = CaptureState(household);

        var repository =
            new RecordingHouseholdRepository(household);

        var handler = new LeaveHouseholdHandler(
            repository,
            new FakeCurrentPerson(Guid.NewGuid()));

        var result = await handler.Handle(
            new LeaveHouseholdCommand(
                household.Id));

        Assert.Equal(
            LeaveHouseholdOutcome.NotFound,
            result.Outcome);

        AssertUnchanged(
            household,
            before);

        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task Handle_owner_returns_invalid_without_update()
    {
        var ownerId = Guid.NewGuid();

        var household = new Household(
            "Home",
            ownerId);

        Assert.True(
            household.AddMember(
                HouseholdRole.Member,
                Guid.NewGuid()).IsSuccess);

        var before = CaptureState(household);

        var repository =
            new RecordingHouseholdRepository(household);

        var handler = new LeaveHouseholdHandler(
            repository,
            new FakeCurrentPerson(ownerId));

        var result = await handler.Handle(
            new LeaveHouseholdCommand(
                household.Id));

        Assert.Equal(
            LeaveHouseholdOutcome.Invalid,
            result.Outcome);

        Assert.Equal(
            "Owner must transfer ownership or close the household before leaving.",
            result.ErrorMessage);

        AssertUnchanged(
            household,
            before);

        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Handle_removes_current_account_and_updates_same_aggregate_once(
        HouseholdRole role)
    {
        var household = new Household(
            "Home",
            Guid.NewGuid());

        var personId = Guid.NewGuid();

        Assert.True(
            household.AddMember(
                role,
                personId).IsSuccess);

        var targetId = household.Members
            .Single(
                member =>
                    member.PersonId == personId)
            .MembershipId;

        var repository =
            new RecordingHouseholdRepository(household);

        var handler = new LeaveHouseholdHandler(
            repository,
            new FakeCurrentPerson(personId));

        using var cancellation =
            new CancellationTokenSource();

        var result = await handler.Handle(
            new LeaveHouseholdCommand(
                household.Id),
            cancellation.Token);

        Assert.Equal(
            LeaveHouseholdOutcome.Success,
            result.Outcome);

        Assert.DoesNotContain(
            household.Members,
            member =>
                member.MembershipId == targetId);

        Assert.Equal(
            HouseholdRole.Owner,
            Assert.Single(household.Members).Role);

        Assert.Equal(1, repository.GetCallCount);

        Assert.Equal(
            household.Id,
            repository.RequestedHouseholdId);

        Assert.Equal(1, repository.UpdateCallCount);

        Assert.Same(
            household,
            repository.UpdatedHousehold);

        Assert.NotNull(
            repository.MembersAtUpdate);

        Assert.DoesNotContain(
            targetId,
            repository.MembersAtUpdate);

        Assert.Equal(
            cancellation.Token,
            repository.GetCancellationToken);

        Assert.Equal(
            cancellation.Token,
            repository.UpdateCancellationToken);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Handle_propagates_repository_exceptions(
        bool failOnUpdate,
        bool unauthorized)
    {
        var personId = Guid.NewGuid();

        var household = new Household(
            "Home",
            Guid.NewGuid());

        Assert.True(
            household.AddMember(
                HouseholdRole.Member,
                personId).IsSuccess);

        Exception expected = unauthorized
            ? new UnauthorizedAccessException(
                "Repository failed")
            : new InvalidOperationException(
                "Repository failed");

        var repository =
            new RecordingHouseholdRepository(household)
            {
                GetException =
                    failOnUpdate ? null : expected,

                UpdateException =
                    failOnUpdate ? expected : null
            };

        var handler = new LeaveHouseholdHandler(
            repository,
            new FakeCurrentPerson(personId));

        var actual =
            await Record.ExceptionAsync(
                () => handler.Handle(
                    new LeaveHouseholdCommand(
                        household.Id)));

        Assert.Same(
            expected,
            actual);

        Assert.Equal(
            1,
            repository.GetCallCount);

        Assert.Equal(
            failOnUpdate ? 1 : 0,
            repository.UpdateCallCount);
    }

    [Fact]
    public async Task Handle_null_command_throws_without_repository_access()
    {
        var repository =
            new RecordingHouseholdRepository();

        var handler = new LeaveHouseholdHandler(
            repository,
            new FakeCurrentPerson(Guid.NewGuid()));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(null!));

        Assert.Equal(0, repository.GetCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    private static (
        Guid Id,
        string Name,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        (
            Guid MembershipId,
            Guid PersonId,
            HouseholdRole Role
        )[] Members)
        CaptureState(Household household)
        => (
            household.Id,
            household.Name,
            household.CreatedAt,
            household.UpdatedAt,
            household.Members
                .OrderBy(
                    member =>
                        member.MembershipId)
                .Select(
                    member =>
                        (
                            member.MembershipId,
                            member.PersonId,
                            member.Role))
                .ToArray());

    private static void AssertUnchanged(
        Household household,
        (
            Guid Id,
            string Name,
            DateTime CreatedAt,
            DateTime UpdatedAt,
            (
                Guid MembershipId,
                Guid PersonId,
                HouseholdRole Role
            )[] Members
        ) before)
    {
        var after = CaptureState(household);

        Assert.Equal(
            before.Id,
            after.Id);

        Assert.Equal(
            before.Name,
            after.Name);

        Assert.Equal(
            before.CreatedAt,
            after.CreatedAt);

        Assert.Equal(
            before.UpdatedAt,
            after.UpdatedAt);

        Assert.Equal(
            before.Members,
            after.Members);
    }

    private sealed class FakeCurrentPerson(Guid? personId) : ICurrentPerson
    {
        public Task<Guid?> GetPersonIdAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(personId);
    }

    private sealed class RecordingHouseholdRepository(
        Household? household = null)
        : IHouseholdRepository
    {
        public Task DeleteAsync(
            Household household,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException(
                "DeleteAsync was not expected to be called.");

        public Guid[]? MembersAtUpdate
        {
            get;
            private set;
        }

        public Guid? RequestedHouseholdId
        {
            get;
            private set;
        }

        public int GetCallCount
        {
            get;
            private set;
        }

        public int UpdateCallCount
        {
            get;
            private set;
        }

        public Household? UpdatedHousehold
        {
            get;
            private set;
        }

        public CancellationToken? GetCancellationToken
        {
            get;
            private set;
        }

        public CancellationToken? UpdateCancellationToken
        {
            get;
            private set;
        }

        public Exception? GetException
        {
            get;
            init;
        }

        public Exception? UpdateException
        {
            get;
            init;
        }

        public Task AddAsync(
            Household addedHousehold,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException(
                "LeaveHousehold must not add a household.");

        public Task<Household?> GetByIdAsync(
            Guid householdId,
            CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            RequestedHouseholdId =
                householdId;

            GetCancellationToken =
                cancellationToken;

            return GetException is null
                ? Task.FromResult(
                    household?.Id == householdId
                        ? household
                        : null)
                : Task.FromException<Household?>(
                    GetException);
        }

        public Task UpdateAsync(
            Household updatedHousehold,
            CancellationToken cancellationToken = default)
        {
            UpdateCallCount++;

            UpdatedHousehold =
                updatedHousehold;

            UpdateCancellationToken =
                cancellationToken;

            MembersAtUpdate =
                updatedHousehold.Members
                    .Select(
                        member =>
                            member.MembershipId)
                    .ToArray();

            return UpdateException is null
                ? Task.CompletedTask
                : Task.FromException(
                    UpdateException);
        }
    }
}
