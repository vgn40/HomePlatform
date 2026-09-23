using HomePlatform.Application.Households.AddHouseholdMemberWithoutAccount;
using HomePlatform.Domain.Household;
using HomePlatform.Domain.Person;

namespace HomePlatform.Infrastructure.Persistence;

public sealed class AddHouseholdMemberWithoutAccountPersistence(
    HomePlatformDbContext context)
    : IAddHouseholdMemberWithoutAccountPersistence
{
    public async Task SaveAsync(
        Person person,
        Household household,
        CancellationToken cancellationToken = default)
    {
        context.Set<Person>().Add(person);
        context.Update(household);

        await context.SaveChangesAsync(cancellationToken);
    }
}