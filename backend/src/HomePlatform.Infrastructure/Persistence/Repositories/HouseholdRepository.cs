using System.Data;
using HomePlatform.Application.Households;
using HomePlatform.Domain.Household;
using HomePlatform.Domain.Person;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HomePlatform.Infrastructure.Persistence.Repositories;

public sealed class HouseholdRepository : IHouseholdRepository
{
    private readonly HomePlatformDbContext _dbContext;

    public HouseholdRepository(HomePlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        Household household,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(household);

        _dbContext.Set<Household>().Add(household);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Household?> GetByIdAsync(
        Guid householdId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .Set<Household>()
            .Include(household => household.Members)
            .SingleOrDefaultAsync(
                household => household.Id == householdId,
                cancellationToken);
    }

    public async Task UpdateAsync(
        Household household,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(household);

        // Force a Household UPDATE so EF Core checks OwnershipVersion,
        // even when only membership data has changed.
        _dbContext.Entry(household).Property(candidate => candidate.UpdatedAt)
            .IsModified = true;

        var removedPersonIds = _dbContext.ChangeTracker
            .Entries<HouseholdMember>()
            .Where(entry =>
                entry.State == EntityState.Deleted
                && entry.Entity.HouseholdId
                    == household.Id)
            .Select(entry => entry.Entity.PersonId)
            .ToArray();

        if (removedPersonIds.Length == 0)
        {
            await SaveChangesAsync(cancellationToken);
            return;
        }

        await SaveWithPersonCleanupAsync(
            () => Task.FromResult(removedPersonIds),
            cancellationToken);
    }

    public async Task DeleteAsync(
        Household household,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(household);

        // Read memberships from the database to account for changes
        // made since the Household was loaded before deleting it.
        await SaveWithPersonCleanupAsync(async () =>
        {
            var personIds = await _dbContext.Set<HouseholdMember>()
                .Where(member => member.HouseholdId == household.Id)
                .Select(member => member.PersonId)
                .ToArrayAsync(cancellationToken);

            _dbContext.Set<Household>().Remove(household);
            return personIds;
        }, cancellationToken);
    }

    private async Task SaveWithPersonCleanupAsync(
        Func<Task<Guid[]>> getPersonIds,
        CancellationToken cancellationToken)
    {
        try
        {
            // Serializable ensures concurrent transactions behave as if executed
            // one at a time, preventing inconsistent Person cleanup across households.
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var personIds = await getPersonIds();
            await SaveChangesAsync(cancellationToken);

            await DeleteUnreferencedPersonsAsync(personIds, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        // EF Core may wrap PostgreSQL concurrency errors in other exceptions.
        // Check the underlying error code to identify serialization failures and deadlocks.
        catch (Exception exception) when (
            exception.GetBaseException() is PostgresException
            {
                SqlState: PostgresErrorCodes.SerializationFailure
                    or PostgresErrorCodes.DeadlockDetected
            })
        {
            throw new HouseholdConcurrencyException(exception);
        }
    }

    private async Task DeleteUnreferencedPersonsAsync(
        Guid[] personIds,
        CancellationToken cancellationToken)
    {
        try
        {
            // Delete only affected Persons with no Account or remaining memberships.
            // Check these conditions directly in SQL to avoid stale application data.
            // Foreign key constraints also protect against concurrent references.
            await _dbContext.Set<Person>()
                .Where(person => personIds.Contains(person.Id)
                    && !_dbContext.Users.Any(user => user.PersonId == person.Id)
                    && !_dbContext.Set<HouseholdMember>()
                        .Any(member => member.PersonId == person.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
        // If a Person DELETE conflicts with an Account or membership reference,
        // treat it as a concurrency conflict. Other FK errors are not handled here.
        catch (PostgresException exception) when (
            exception.SqlState == PostgresErrorCodes.ForeignKeyViolation
            && exception.ConstraintName is "FK_AspNetUsers_Person_PersonId"
                or "FK_HouseholdMember_Person_PersonId")
        {
            throw new HouseholdConcurrencyException(exception);
        }
    }

    private async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new HouseholdConcurrencyException(exception);
        }
    }
}
