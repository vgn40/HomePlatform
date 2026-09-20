using HomePlatform.Application.Households;
using HomePlatform.Application.Households.CloseHousehold;
using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Tests.Households.CloseHousehold;

public sealed class CloseHouseholdHandlerTests
{
    [Fact]
    public async Task Handle_unauthenticated_does_not_load_or_delete()
    {
        var repository = new RecordingHouseholdRepository();

        var handler = new CloseHouseholdHandler(
            repository,
            new UnauthenticatedCurrentAccount());

        var result = await handler.Handle(
            new CloseHouseholdCommand(Guid.NewGuid()));

        Assert.Equal(
            CloseHouseholdOutcome.Unauthenticated,
            result.Outcome);

        Assert.Equal(0, repository.GetCallCount);
        Assert.Equal(0, repository.DeleteCallCount);
    }

    [Fact]
    public async Task Handle_missing_household_returns_not_found_without_delete()
    {
        var repository = new RecordingHouseholdRepository();
        var id = Guid.NewGuid();

        var handler = new CloseHouseholdHandler(
            repository,
            new FakeCurrentAccount(Guid.NewGuid()));

        var result = await handler.Handle(
            new CloseHouseholdCommand(id));

        Assert.Equal(
            CloseHouseholdOutcome.NotFound,
            result.Outcome);

        Assert.Equal(
            id,
            repository.RequestedHouseholdId);

        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(0, repository.DeleteCallCount);
    }

    [Fact]
    public async Task Handle_non_member_returns_not_found_without_delete()
    {
        var accountId = Guid.NewGuid();
        var household = new Household(
            "Home",
            Guid.NewGuid());

        Assert.True(
            household.AddMember(
                HouseholdRole.Guest).IsSuccess);

        var before = CaptureState(household);

        var repository =
            new RecordingHouseholdRepository(household);

        var handler = new CloseHouseholdHandler(
            repository,
            new FakeCurrentAccount(accountId));

        var result = await handler.Handle(
            new CloseHouseholdCommand(
                household.Id));

        Assert.Equal(
            CloseHouseholdOutcome.NotFound,
            result.Outcome);

        AssertUnchanged(
            household,
            before);

        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(0, repository.DeleteCallCount);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Handle_non_owner_member_returns_forbidden_without_delete(
        HouseholdRole role)
    {
        var accountId = Guid.NewGuid();

        var household = new Household(
            "Home",
            Guid.NewGuid());

        Assert.True(
            household.AddMember(
                role,
                accountId).IsSuccess);

        Assert.True(
            household.AddMember(
                HouseholdRole.Guest).IsSuccess);

        var before = CaptureState(household);

        var repository =
            new RecordingHouseholdRepository(household);

        var handler = new CloseHouseholdHandler(
            repository,
            new FakeCurrentAccount(accountId));

        var result = await handler.Handle(
            new CloseHouseholdCommand(
                household.Id));

        Assert.Equal(
            CloseHouseholdOutcome.Forbidden,
            result.Outcome);

        AssertUnchanged(
            household,
            before);

        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(0, repository.DeleteCallCount);
    }

    [Fact]
    public async Task Handle_owner_deletes_same_aggregate_once_after_read_and_forwards_token()
    {
        var accountId = Guid.NewGuid();

        var household = new Household(
            "Home",
            accountId);

        Assert.True(
            household.AddMember(
                HouseholdRole.Member,
                Guid.NewGuid()).IsSuccess);

        var before = CaptureState(household);

        var repository =
            new RecordingHouseholdRepository(household);

        var handler = new CloseHouseholdHandler(
            repository,
            new FakeCurrentAccount(accountId));

        using var cancellation =
            new CancellationTokenSource();

        var result = await handler.Handle(
            new CloseHouseholdCommand(
                household.Id),
            cancellation.Token);

        Assert.Equal(
            CloseHouseholdOutcome.Success,
            result.Outcome);

        Assert.Equal(1, repository.GetCallCount);

        Assert.Equal(
            household.Id,
            repository.RequestedHouseholdId);

        Assert.Equal(1, repository.DeleteCallCount);

        Assert.Same(
            household,
            repository.DeletedHousehold);

        Assert.Equal(
            new[] { "get", "delete" },
            repository.Calls);

        Assert.Equal(
            cancellation.Token,
            repository.GetCancellationToken);

        Assert.Equal(
            cancellation.Token,
            repository.DeleteCancellationToken);

        AssertUnchanged(
            household,
            before);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Handle_propagates_repository_exceptions(
        bool failOnDelete,
        bool unauthorized)
    {
        var accountId = Guid.NewGuid();

        var household = new Household(
            "Home",
            accountId);

        Exception expected = unauthorized
            ? new UnauthorizedAccessException(
                "Repository failed")
            : new InvalidOperationException(
                "Repository failed");

        var repository =
            new RecordingHouseholdRepository(household)
            {
                GetException =
                    failOnDelete ? null : expected,

                DeleteException =
                    failOnDelete ? expected : null
            };

        var handler = new CloseHouseholdHandler(
            repository,
            new FakeCurrentAccount(accountId));

        var actual =
            await Record.ExceptionAsync(
                () => handler.Handle(
                    new CloseHouseholdCommand(
                        household.Id)));

        Assert.Same(
            expected,
            actual);

        Assert.Equal(
            1,
            repository.GetCallCount);

        Assert.Equal(
            failOnDelete ? 1 : 0,
            repository.DeleteCallCount);
    }

    [Fact]
    public async Task Handle_null_command_throws_without_repository_access()
    {
        var repository =
            new RecordingHouseholdRepository();

        var handler = new CloseHouseholdHandler(
            repository,
            new FakeCurrentAccount(Guid.NewGuid()));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.Handle(null!));

        Assert.Equal(0, repository.GetCallCount);
        Assert.Equal(0, repository.DeleteCallCount);
    }

    private static (
        Guid Id,
        string Name,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        (
            Guid MembershipId,
            Guid? AccountId,
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
                            member.AccountId,
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
                Guid? AccountId,
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

    private sealed class FakeCurrentAccount(
        Guid accountId)
        : ICurrentAccount
    {
        public Guid AccountId { get; } =
            accountId;
    }

    private sealed class UnauthenticatedCurrentAccount
        : ICurrentAccount
    {
        public Guid AccountId =>
            throw new UnauthorizedAccessException();
    }

    private sealed class RecordingHouseholdRepository(
        Household? household = null)
        : IHouseholdRepository
    {
        public Task UpdateAsync(
            Household household,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException(
                "UpdateAsync was not expected to be called.");

        public List<string> Calls { get; } =
            new();

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

        public int DeleteCallCount
        {
            get;
            private set;
        }

        public Household? DeletedHousehold
        {
            get;
            private set;
        }

        public CancellationToken? GetCancellationToken
        {
            get;
            private set;
        }

        public CancellationToken? DeleteCancellationToken
        {
            get;
            private set;
        }

        public Exception? GetException
        {
            get;
            init;
        }

        public Exception? DeleteException
        {
            get;
            init;
        }

        public Task AddAsync(
            Household addedHousehold,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException(
                "CloseHousehold must not add a household.");

        public Task<Household?> GetByIdAsync(
            Guid householdId,
            CancellationToken cancellationToken = default)
        {
            Calls.Add("get");

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

        public Task DeleteAsync(
            Household deletedHousehold,
            CancellationToken cancellationToken = default)
        {
            Calls.Add("delete");

            DeleteCallCount++;

            DeletedHousehold =
                deletedHousehold;

            DeleteCancellationToken =
                cancellationToken;

            return DeleteException is null
                ? Task.CompletedTask
                : Task.FromException(
                    DeleteException);
        }
    }
}