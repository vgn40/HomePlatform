using HomePlatform.Application.Households;
using HomePlatform.Application.Households.GetHousehold;
using HomePlatform.Application.Households.ListHouseholds;
using HomePlatform.Domain.Household;
using HomePlatform.Domain.Person;
using Microsoft.EntityFrameworkCore;

namespace HomePlatform.Infrastructure.Persistence;

public sealed class HouseholdQueryService(HomePlatformDbContext context)
    : IHouseholdQueryService
{
    public async Task<IReadOnlyList<HouseholdSummaryResponse>> ListForPersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
        => await context.Set<Household>()
            .AsNoTracking()
            .Where(household => household.Members.Any(member => member.PersonId == personId))
            .OrderBy(household => household.Name)
            .ThenBy(household => household.Id)
            .Select(household => new HouseholdSummaryResponse(
                household.Id,
                household.Name,
                household.Members.Where(member => member.PersonId == personId)
                    .Select(member => member.Role).First(),
                household.Members.Count))
            .ToListAsync(cancellationToken);

    public Task<HouseholdDetailResponse?> GetForPersonAsync(
        Guid householdId,
        Guid personId,
        CancellationToken cancellationToken = default)
        => context.Set<Household>()
            .AsNoTracking()
            .AsSingleQuery()
            .Where(household => household.Id == householdId
                && household.Members.Any(member => member.PersonId == personId))
            .Select(household => new HouseholdDetailResponse(
                household.Id,
                household.Name,
                household.CreatedAt,
                household.UpdatedAt,
                household.Members.Where(member => member.PersonId == personId)
                    .Select(member => member.Role).First(),
                (from member in household.Members
                 join person in context.Set<Person>() on member.PersonId equals person.Id
                 orderby member.MembershipId
                 select new HouseholdMemberResponse(
                     member.MembershipId,
                     member.PersonId,
                     person.DisplayName,
                     member.Role)).ToList()))
            .SingleOrDefaultAsync(cancellationToken);
}
