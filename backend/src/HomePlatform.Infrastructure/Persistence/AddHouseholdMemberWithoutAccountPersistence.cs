using HomePlatform.Application.Households;
using HomePlatform.Application.Households.AddHouseholdMemberWithoutAccount;
using HomePlatform.Domain.Household;
using HomePlatform.Domain.Person;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        // Always validate the loaded ownership token, even if UpdatedAt did not
        // change at the clock's resolution. Keep existing memberships unchanged.
        context.Entry(household).Property(candidate => candidate.UpdatedAt)
            .IsModified = true;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new HouseholdConcurrencyException(exception);
        }
        // A concurrent close can delete the parent before EF inserts the child.
        // Only this FK denotes that race; other database failures must propagate.
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException postgresException
            && postgresException.SqlState ==
                PostgresErrorCodes.ForeignKeyViolation
            && postgresException.ConstraintName ==
                "FK_HouseholdMember_Household_HouseholdId")
        {
            throw new HouseholdConcurrencyException(exception);
        }
    }
}
