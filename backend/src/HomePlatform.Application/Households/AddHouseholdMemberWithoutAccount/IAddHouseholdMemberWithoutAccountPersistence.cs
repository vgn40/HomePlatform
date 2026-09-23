using HomePlatform.Domain.Household;
using HomePlatform.Domain.Person;

namespace HomePlatform.Application.Households.AddHouseholdMemberWithoutAccount;

public interface IAddHouseholdMemberWithoutAccountPersistence
{
    Task SaveAsync(
        Person person,
        Household household,
        CancellationToken cancellationToken = default);
}