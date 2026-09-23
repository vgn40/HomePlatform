using HomePlatform.Application.Identity;
using HomePlatform.Domain.Household;
using HomePlatform.Domain.Person;

namespace HomePlatform.Application.Households.AddHouseholdMemberWithoutAccount;

public sealed class AddHouseholdMemberWithoutAccountHandler
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly ICurrentPerson _currentPerson;
    private readonly IAddHouseholdMemberWithoutAccountPersistence _persistence;

    public AddHouseholdMemberWithoutAccountHandler(
        IHouseholdRepository householdRepository,
        ICurrentPerson currentPerson,
        IAddHouseholdMemberWithoutAccountPersistence persistence)
    {
        _householdRepository = householdRepository;
        _currentPerson = currentPerson;
        _persistence = persistence;
    }

    public async Task<AddHouseholdMemberWithoutAccountResult> Handle(
        AddHouseholdMemberWithoutAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var currentPersonId =
            await _currentPerson.GetPersonIdAsync(cancellationToken);

        if (currentPersonId is null)
        {
            return AddHouseholdMemberWithoutAccountResult.Unauthenticated();
        }

        var household = await _householdRepository.GetByIdAsync(
            command.HouseholdId,
            cancellationToken);

        if (household is null)
        {
            return AddHouseholdMemberWithoutAccountResult.NotFound();
        }

        Person person;

        try
        {
            person = new Person(command.DisplayName);
        }
        catch (ArgumentException exception)
        {
            return AddHouseholdMemberWithoutAccountResult.Invalid(
                exception.Message);
        }

        AddHouseholdMemberDomainResult addResult;

        try
        {
            addResult = household.AddMember(
                currentPersonId.Value,
                command.Role,
                person.Id);
        }
        catch (ArgumentException exception)
        {
            return AddHouseholdMemberWithoutAccountResult.Invalid(
                exception.Message);
        }

        if (!addResult.IsSuccess)
        {
            return addResult.Error switch
            {
                AddHouseholdMemberError.CurrentPersonNotMember =>
                    AddHouseholdMemberWithoutAccountResult.NotFound(),

                AddHouseholdMemberError.CurrentPersonNotOwner =>
                    AddHouseholdMemberWithoutAccountResult.Forbidden(),

                AddHouseholdMemberError.CannotAddOwner =>
                    AddHouseholdMemberWithoutAccountResult.Invalid(
                        "A new household member cannot be added as owner."),

                AddHouseholdMemberError.PersonAlreadyMember =>
                    AddHouseholdMemberWithoutAccountResult.Invalid(
                        "Person is already a member of this household."),

                _ => throw new InvalidOperationException(
                    "Unexpected AddHouseholdMember domain error.")
            };
        }

        var membership = household.Members.Single(
            member => member.PersonId == person.Id);

        await _persistence.SaveAsync(
            person,
            household,
            cancellationToken);

        return AddHouseholdMemberWithoutAccountResult.Success(
            person.Id,
            membership.MembershipId);
    }
}