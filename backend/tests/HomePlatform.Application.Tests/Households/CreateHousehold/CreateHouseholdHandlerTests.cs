using HomePlatform.Application.Households;
using HomePlatform.Application.Households.CreateHousehold;
using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;

namespace HomePlatform.Application.Tests.Households.CreateHousehold;

public sealed class CreateHouseholdHandlerTests
{
    [Fact]
    public async Task Handle_creates_and_adds_household_and_returns_its_identity()
    {
        var accountId = Guid.NewGuid();
        var repository = new RecordingHouseholdRepository();
        var handler = new CreateHouseholdHandler(
            repository,
            new FakeCurrentAccount(accountId));
        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await handler.Handle(
            new CreateHouseholdCommand("  Mit hjem  "),
            cancellationTokenSource.Token);

        var household = Assert.Single(repository.AddedHouseholds);
        var owner = Assert.Single(household.Members);
        Assert.Equal(CreateHouseholdOutcome.Success, result.Outcome);
        Assert.Equal("Mit hjem", household.Name);
        Assert.Equal(accountId, owner.AccountId);
        Assert.Equal(HouseholdRole.Owner, owner.Role);
        Assert.Equal(household.Id, result.HouseholdId);
        Assert.Equal(household.Name, result.Name);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(cancellationTokenSource.Token, repository.CancellationToken);
    }

    [Fact]
    public async Task Handle_with_null_command_throws_without_repository_write()
    {
        var repository = new RecordingHouseholdRepository();
        var handler = CreateHandler(repository);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.Handle(null!));

        Assert.Empty(repository.AddedHouseholds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_with_invalid_name_returns_invalid_without_repository_write(
        string? name)
    {
        var repository = new RecordingHouseholdRepository();
        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new CreateHouseholdCommand(name!));

        Assert.Equal(CreateHouseholdOutcome.Invalid, result.Outcome);
        Assert.Equal(
            "Household name cannot be empty. (Parameter 'name')",
            result.ErrorMessage);
        Assert.Null(result.HouseholdId);
        Assert.Null(result.Name);
        Assert.Empty(repository.AddedHouseholds);
    }

    [Fact]
    public async Task Handle_with_unauthenticated_current_account_returns_unauthenticated_without_repository_write()
    {
        var repository = new RecordingHouseholdRepository();
        var handler = new CreateHouseholdHandler(
            repository,
            new UnauthenticatedCurrentAccount());

        var result = await handler.Handle(
            new CreateHouseholdCommand("Mit hjem"));

        Assert.Equal(CreateHouseholdOutcome.Unauthenticated, result.Outcome);
        Assert.Null(result.HouseholdId);
        Assert.Null(result.Name);
        Assert.Null(result.ErrorMessage);
        Assert.Empty(repository.AddedHouseholds);
    }

    [Fact]
    public async Task Handle_propagates_repository_failure()
    {
        var expectedException = new InvalidOperationException("Write failed.");
        var repository = new RecordingHouseholdRepository(expectedException);
        var handler = CreateHandler(repository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new CreateHouseholdCommand("Mit hjem")));

        Assert.Same(expectedException, exception);
        Assert.Single(repository.AddedHouseholds);
    }

    private static CreateHouseholdHandler CreateHandler(
        RecordingHouseholdRepository repository)
    {
        return new CreateHouseholdHandler(
            repository,
            new FakeCurrentAccount(Guid.NewGuid()));
    }

    private sealed class FakeCurrentAccount(Guid accountId) : ICurrentAccount
    {
        public Guid AccountId { get; } = accountId;
    }

    private sealed class UnauthenticatedCurrentAccount : ICurrentAccount
    {
        public Guid AccountId => throw new UnauthorizedAccessException();
    }

    private sealed class RecordingHouseholdRepository(
        Exception? exceptionToThrow = null) : IHouseholdRepository
    {
        public List<Household> AddedHouseholds { get; } = new();
        public CancellationToken? CancellationToken { get; private set; }

        public Task<Household?> GetByIdAsync(
            Guid householdId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("CreateHousehold must not read an existing household.");
        }

        public Task UpdateAsync(
            Household household,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("CreateHousehold must not update an existing household.");
        }

        public Task AddAsync(
            Household household,
            CancellationToken cancellationToken = default)
        {
            AddedHouseholds.Add(household);
            CancellationToken = cancellationToken;

            return exceptionToThrow is null
                ? Task.CompletedTask
                : Task.FromException(exceptionToThrow);
        }
    }
}
