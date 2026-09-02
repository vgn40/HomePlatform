using HomePlatform.Domain.Household;
using HomePlatform.Application.Identity;
namespace HomePlatform.Application.Households.CreateHousehold;

public sealed class CreateHouseholdHandler
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentAccount _currentAccount;

    public CreateHouseholdHandler(
        IHouseholdRepository householdRepository,
        ICurrentAccount currentAccount)
    {
        _householdRepository = householdRepository;
        _currentAccount = currentAccount;
    }

    public async Task<CreateHouseholdResult> Handle(
        CreateHouseholdCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        Guid accountId;

        try
        {
            accountId = _currentAccount.AccountId;
        }
        catch (UnauthorizedAccessException)
        {
            return CreateHouseholdResult.Unauthenticated();
        }

        Household household;

        try
        {
            household = new Household(
                command.Name,
                accountId);
        }
        catch (ArgumentException exception)
        {
            return CreateHouseholdResult.Invalid(
                exception.Message);
        }

        await _householdRepository.AddAsync(
            household,
            cancellationToken);

        return CreateHouseholdResult.Success(
            household.Id,
            household.Name);
    }
}
