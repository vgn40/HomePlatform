using HomePlatform.Application.Households;
using HomePlatform.Application.Households.TransferOwnership;
using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Tests.Households.TransferOwnership;

public sealed class TransferOwnershipHandlerTests
{
    [Fact]
    public async Task Handle_with_unauthenticated_account_does_not_read_or_update()
    {
        var repository = new RecordingHouseholdRepository();
        var handler = new TransferOwnershipHandler(repository, new UnauthenticatedCurrentAccount());

        var result = await handler.Handle(new TransferOwnershipCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(TransferOwnershipOutcome.Unauthenticated, result.Outcome);
        Assert.Equal(0, repository.GetCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task Handle_with_missing_household_returns_not_found_without_update()
    {
        var repository = new RecordingHouseholdRepository();
        var householdId = Guid.NewGuid();
        var handler = CreateHandler(repository, Guid.NewGuid());

        var result = await handler.Handle(new TransferOwnershipCommand(householdId, Guid.NewGuid()));

        Assert.Equal(TransferOwnershipOutcome.NotFound, result.Outcome);
        Assert.Equal(householdId, repository.RequestedHouseholdId);
        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task Handle_with_non_member_returns_forbidden_without_update()
    {
        var household = new Household("Mit hjem", Guid.NewGuid());
        var target = AddMember(household, HouseholdRole.Member, Guid.NewGuid());
        var repository = new RecordingHouseholdRepository(household);
        var handler = CreateHandler(repository, Guid.NewGuid());
        var before = CaptureState(household);

        var result = await handler.Handle(new TransferOwnershipCommand(household.Id, target.MembershipId));

        Assert.Equal(TransferOwnershipOutcome.Forbidden, result.Outcome);
        AssertUnchanged(household, before);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Handle_with_non_owner_returns_forbidden_without_update(HouseholdRole callerRole)
    {
        var household = new Household("Mit hjem", Guid.NewGuid());
        var callerAccountId = Guid.NewGuid();
        AddMember(household, callerRole, callerAccountId);
        var target = AddMember(household, HouseholdRole.Member, Guid.NewGuid());
        var repository = new RecordingHouseholdRepository(household);
        var handler = CreateHandler(repository, callerAccountId);
        var before = CaptureState(household);

        var result = await handler.Handle(new TransferOwnershipCommand(household.Id, target.MembershipId));

        Assert.Equal(TransferOwnershipOutcome.Forbidden, result.Outcome);
        AssertUnchanged(household, before);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Theory]
    [InlineData("Missing")]
    [InlineData("Loginless")]
    [InlineData("Self")]
    public async Task Handle_with_invalid_target_leaves_aggregate_unchanged_without_update(string targetKind)
    {
        var ownerAccountId = Guid.NewGuid();
        var household = new Household("Mit hjem", ownerAccountId);
        var owner = Assert.Single(household.Members);
        AddMember(household, HouseholdRole.Member, Guid.NewGuid());
        var loginless = AddMember(household, HouseholdRole.Member, null);
        var targetId = targetKind switch
        {
            "Missing" => Guid.NewGuid(),
            "Loginless" => loginless.MembershipId,
            "Self" => owner.MembershipId,
            _ => throw new ArgumentOutOfRangeException(nameof(targetKind))
        };
        var repository = new RecordingHouseholdRepository(household);
        var handler = CreateHandler(repository, ownerAccountId);
        var before = CaptureState(household);

        var result = await handler.Handle(new TransferOwnershipCommand(household.Id, targetId));

        Assert.Equal(TransferOwnershipOutcome.Invalid, result.Outcome);
        AssertUnchanged(household, before);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Handle_transfers_ownership_and_updates_same_aggregate_once(HouseholdRole targetRole)
    {
        var ownerAccountId = Guid.NewGuid();
        var household = new Household("Mit hjem", ownerAccountId);
        var owner = Assert.Single(household.Members);
        var target = AddMember(household, targetRole, Guid.NewGuid());
        var repository = new RecordingHouseholdRepository(household);
        var handler = CreateHandler(repository, ownerAccountId);

        TransferOwnershipResult result = await handler.Handle(
            new TransferOwnershipCommand(household.Id, target.MembershipId));

        Assert.Equal(TransferOwnershipOutcome.Success, result.Outcome);
        Assert.Equal(HouseholdRole.Member, owner.Role);
        Assert.Equal(HouseholdRole.Owner, target.Role);
        Assert.Equal(household.Id, repository.RequestedHouseholdId);
        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(1, repository.UpdateCallCount);
        Assert.Same(household, repository.UpdatedHousehold);
        Assert.Equal(HouseholdRole.Member, repository.OldOwnerRoleAtUpdate);
        Assert.Equal(HouseholdRole.Owner, repository.TargetRoleAtUpdate);
    }

    [Fact]
    public async Task Handle_forwards_cancellation_token_to_read_and_update()
    {
        var ownerAccountId = Guid.NewGuid();
        var household = new Household("Mit hjem", ownerAccountId);
        var target = AddMember(household, HouseholdRole.Member, Guid.NewGuid());
        var repository = new RecordingHouseholdRepository(household);
        var handler = CreateHandler(repository, ownerAccountId);
        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await handler.Handle(
            new TransferOwnershipCommand(household.Id, target.MembershipId), cancellationTokenSource.Token);

        Assert.Equal(TransferOwnershipOutcome.Success, result.Outcome);
        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(1, repository.UpdateCallCount);
        Assert.Equal(cancellationTokenSource.Token, repository.GetCancellationToken);
        Assert.Equal(cancellationTokenSource.Token, repository.UpdateCancellationToken);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Handle_propagates_repository_exceptions(bool failOnUpdate, bool unauthorized)
    {
        var ownerAccountId = Guid.NewGuid();
        var household = new Household("Mit hjem", ownerAccountId);
        var target = AddMember(household, HouseholdRole.Member, Guid.NewGuid());
        Exception expected = unauthorized
            ? new UnauthorizedAccessException("Repository access failed.")
            : new InvalidOperationException("Repository failed.");
        var repository = new RecordingHouseholdRepository(household)
        {
            GetException = failOnUpdate ? null : expected,
            UpdateException = failOnUpdate ? expected : null
        };
        var handler = CreateHandler(repository, ownerAccountId);

        var actual = await Record.ExceptionAsync(() => handler.Handle(
            new TransferOwnershipCommand(household.Id, target.MembershipId)));

        Assert.Same(expected, actual);
        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(failOnUpdate ? 1 : 0, repository.UpdateCallCount);
    }

    [Fact]
    public async Task Handle_with_null_command_throws_without_repository_access()
    {
        var repository = new RecordingHouseholdRepository();
        var handler = CreateHandler(repository, Guid.NewGuid());

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(null!));

        Assert.Equal(0, repository.GetCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    [Theory]
    [InlineData(HouseholdRole.Member, false)]
    [InlineData(HouseholdRole.Member, true)]
    [InlineData(HouseholdRole.Guest, false)]
    [InlineData(HouseholdRole.Guest, true)]
    [InlineData(null, false)]
    [InlineData(null, true)]
    public async Task Handle_with_unauthorized_caller_and_invalid_target_returns_forbidden(
        HouseholdRole? callerRole,
        bool loginlessTarget)
    {
        var household = new Household("Mit hjem", Guid.NewGuid());
        var callerAccountId = Guid.NewGuid();
        if (callerRole is HouseholdRole role)
        {
            AddMember(household, role, callerAccountId);
        }

        var targetId = loginlessTarget
            ? AddMember(household, HouseholdRole.Member, null).MembershipId
            : Guid.NewGuid();
        var repository = new RecordingHouseholdRepository(household);
        var handler = CreateHandler(repository, callerAccountId);
        var before = CaptureState(household);

        var result = await handler.Handle(new TransferOwnershipCommand(household.Id, targetId));

        Assert.Equal(TransferOwnershipOutcome.Forbidden, result.Outcome);
        AssertUnchanged(household, before);
        Assert.Equal(1, repository.GetCallCount);
        Assert.Equal(0, repository.UpdateCallCount);
    }

    private static TransferOwnershipHandler CreateHandler(RecordingHouseholdRepository repository, Guid accountId)
        => new(repository, new FakeCurrentAccount(accountId));

    private static HouseholdMember AddMember(Household household, HouseholdRole role, Guid? accountId)
    {
        Assert.True(household.AddMember(role, accountId).IsSuccess);
        return Assert.Single(household.Members, member => member.AccountId == accountId);
    }

    private static (Guid Id, string Name, DateTime CreatedAt, DateTime UpdatedAt,
        (Guid MembershipId, Guid? AccountId, HouseholdRole Role)[] Members) CaptureState(Household household)
        => (household.Id, household.Name, household.CreatedAt, household.UpdatedAt,
            household.Members.OrderBy(member => member.MembershipId)
                .Select(member => (member.MembershipId, member.AccountId, member.Role)).ToArray());

    private static void AssertUnchanged(Household household,
        (Guid Id, string Name, DateTime CreatedAt, DateTime UpdatedAt,
        (Guid MembershipId, Guid? AccountId, HouseholdRole Role)[] Members) before)
    {
        var after = CaptureState(household);
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(before.Name, after.Name);
        Assert.Equal(before.CreatedAt, after.CreatedAt);
        Assert.Equal(before.UpdatedAt, after.UpdatedAt);
        Assert.Equal(before.Members, after.Members);
    }

    private sealed class FakeCurrentAccount(Guid accountId) : ICurrentAccount
    {
        public Guid AccountId { get; } = accountId;
    }

    private sealed class UnauthenticatedCurrentAccount : ICurrentAccount
    {
        public Guid AccountId => throw new UnauthorizedAccessException();
    }

    private sealed class RecordingHouseholdRepository(Household? household = null) : IHouseholdRepository
    {
        private readonly Guid? _oldOwnerMembershipId = household?.Members
            .Single(member => member.Role == HouseholdRole.Owner).MembershipId;
        private readonly Guid? _targetMembershipId = household?.Members
            .FirstOrDefault(member => member.Role != HouseholdRole.Owner)?.MembershipId;

        public Guid? RequestedHouseholdId { get; private set; }
        public int GetCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public Household? UpdatedHousehold { get; private set; }
        public CancellationToken? GetCancellationToken { get; private set; }
        public CancellationToken? UpdateCancellationToken { get; private set; }
        public HouseholdRole? OldOwnerRoleAtUpdate { get; private set; }
        public HouseholdRole? TargetRoleAtUpdate { get; private set; }
        public Exception? GetException { get; init; }
        public Exception? UpdateException { get; init; }

        public Task AddAsync(Household addedHousehold, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("TransferOwnership must not add a household.");

        public Task<Household?> GetByIdAsync(Guid householdId, CancellationToken cancellationToken = default)
        {
            GetCallCount++;
            RequestedHouseholdId = householdId;
            GetCancellationToken = cancellationToken;
            return GetException is null
                ? Task.FromResult(household?.Id == householdId ? household : null)
                : Task.FromException<Household?>(GetException);
        }

        public Task UpdateAsync(Household updatedHousehold, CancellationToken cancellationToken = default)
        {
            UpdateCallCount++;
            UpdatedHousehold = updatedHousehold;
            UpdateCancellationToken = cancellationToken;
            OldOwnerRoleAtUpdate = updatedHousehold.Members
                .SingleOrDefault(member => member.MembershipId == _oldOwnerMembershipId)?.Role;
            TargetRoleAtUpdate = updatedHousehold.Members
                .SingleOrDefault(member => member.MembershipId == _targetMembershipId)?.Role;
            return UpdateException is null ? Task.CompletedTask : Task.FromException(UpdateException);
        }
    }
}
