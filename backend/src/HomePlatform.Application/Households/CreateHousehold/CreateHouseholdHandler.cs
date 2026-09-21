using HomePlatform.Domain.Household;
using HomePlatform.Application.Identity;
namespace HomePlatform.Application.Households.CreateHousehold;

public sealed class CreateHouseholdHandler
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentPerson _currentPerson;

    public CreateHouseholdHandler(
        IHouseholdRepository householdRepository,
        ICurrentPerson currentPerson)
    {
        _householdRepository = householdRepository;
        _currentPerson = currentPerson;
    }

    public async Task<CreateHouseholdResult> Handle(
        CreateHouseholdCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var personId = await _currentPerson.GetPersonIdAsync(cancellationToken);
        if (personId is null)
        {
            return CreateHouseholdResult.Unauthenticated();
        }

        Household household;

        try
        {
            household = new Household(
                command.Name,
                personId.Value);
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
