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

        var household = new Household(
            command.Name,
            _currentAccount.AccountId);

        await _householdRepository.AddAsync(
            household,
            cancellationToken);

        return new CreateHouseholdResult(
            household.Id,
            household.Name);
    }
}