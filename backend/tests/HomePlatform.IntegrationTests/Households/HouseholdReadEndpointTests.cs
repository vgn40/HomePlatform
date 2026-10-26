using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomePlatform.Application.Households.GetHousehold;
using HomePlatform.Application.Households.ListHouseholds;
using HomePlatform.Domain.Household;
using HomePlatform.Domain.Person;
using HomePlatform.Infrastructure.Identity;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HomePlatform.IntegrationTests.Households;

public sealed class HouseholdReadEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlTestDatabase _postgres = new();
    private readonly ReadCounter _reads = new();
    private HomePlatformApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new HomePlatformApiFactory(_postgres.GetConnectionString(), services =>
            services.AddDbContext<HomePlatformDbContext>(options => options.AddInterceptors(_reads)));
        await using var scope = _factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>().Database.MigrateAsync();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task List_returns_only_current_persons_households_with_each_role_and_member_count()
    {
        var actor = new Person("Actor");
        var other = new Person("Other");
        var accountId = await CreateAccountAsync(actor);
        // A Person whose ID equals the Account ID must not become the actor.
        var accountIdPerson = new Person();
        var ownerHome = new Household("A home", actor.Id);
        var memberHome = new Household("B home", other.Id);
        var guestHome = new Household("C home", other.Id);
        Assert.True(ownerHome.AddMember(actor.Id, HouseholdRole.Guest, other.Id).IsSuccess);
        Assert.True(memberHome.AddMember(other.Id, HouseholdRole.Member, actor.Id).IsSuccess);
        Assert.True(guestHome.AddMember(other.Id, HouseholdRole.Guest, actor.Id).IsSuccess);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
            db.Entry(accountIdPerson).Property(person => person.Id).CurrentValue = accountId;
            db.AddRange(other, accountIdPerson);
            db.AddRange(ownerHome, memberHome, guestHome, new Household("Hidden", accountId));
            await db.SaveChangesAsync();
        }
        _reads.Count = 0;
        using var request = Request($"/api/households?personId={accountId}", accountId);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HouseholdSummaryResponse[]>();
        Assert.NotNull(body);
        Assert.Equal(new[]
        {
            new HouseholdSummaryResponse(ownerHome.Id, "A home", HouseholdRole.Owner, 2),
            new HouseholdSummaryResponse(memberHome.Id, "B home", HouseholdRole.Member, 2),
            new HouseholdSummaryResponse(guestHome.Id, "C home", HouseholdRole.Guest, 2)
        }, body);
        // One existing identity lookup plus one projected Household query.
        Assert.Equal(2, _reads.Count);
    }

    [Fact]
    public async Task List_without_memberships_returns_200_empty_array()
    {
        var accountId = await CreateAccountAsync(new Person());
        using var request = Request("/api/households", accountId);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Detail_returns_404_for_missing_household_or_nonmember_even_with_supplied_person_id(bool missing)
    {
        var owner = new Person("Owner");
        var actorId = await CreateAccountAsync(new Person());
        var household = new Household("Private", owner.Id);
        await SaveAsync(owner, household);
        using var request = Request(
            $"/api/households/{(missing ? Guid.NewGuid() : household.Id)}?personId={owner.Id}", actorId);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("Private", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(HouseholdRole.Owner)]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Detail_returns_directory_for_every_member_role_with_person_display_names(HouseholdRole role)
    {
        var owner = new Person("Owner");
        var member = new Person("Alma");
        var guest = new Person();
        var actor = role == HouseholdRole.Owner ? owner : role == HouseholdRole.Member ? member : guest;
        var accountId = await CreateAccountAsync(actor);
        var household = new Household("Home", owner.Id);
        Assert.True(household.AddMember(owner.Id, HouseholdRole.Member, member.Id).IsSuccess);
        Assert.True(household.AddMember(owner.Id, HouseholdRole.Guest, guest.Id).IsSuccess);
        await SaveAsync(new[] { owner, member, guest }.Where(person => person != actor)
            .Cast<object>().Append(household).ToArray());
        _reads.Count = 0;
        using var request = Request($"/api/households/{household.Id}", accountId);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HouseholdDetailResponse>();
        Assert.NotNull(body);
        Assert.Equal(household.Id, body.HouseholdId);
        Assert.Equal("Home", body.Name);
        Assert.Equal(ToPostgresPrecision(household.CreatedAt), body.CreatedAt);
        Assert.Equal(ToPostgresPrecision(household.UpdatedAt), body.UpdatedAt);
        Assert.Equal(role, body.Role);
        Assert.Equal(3, body.Members.Count);
        foreach (var person in new[] { owner, member, guest })
        {
            var expected = Assert.Single(household.Members, membership => membership.PersonId == person.Id);
            Assert.Equal(new HouseholdMemberResponse(expected.MembershipId, person.Id,
                person.DisplayName, expected.Role), Assert.Single(body.Members, item => item.PersonId == person.Id));
        }
        Assert.Equal(2, _reads.Count);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "createdAt", "householdId", "members", "name", "role", "updatedAt" },
            json.RootElement.EnumerateObject().Select(property => property.Name).Order());
        foreach (var item in json.RootElement.GetProperty("members").EnumerateArray())
        {
            Assert.Equal(new[] { "displayName", "membershipId", "personId", "role" },
                item.EnumerateObject().Select(property => property.Name).Order());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unknown_account_returns_401(bool detail)
    {
        using var request = Request(detail ? $"/api/households/{Guid.NewGuid()}" : "/api/households", Guid.NewGuid());

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<Guid> CreateAccountAsync(Person person)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        db.Add(person);
        var accountId = Guid.NewGuid();
        var result = await manager.CreateAsync(new ApplicationUser
        {
            Id = accountId,
            PersonId = person.Id,
            UserName = $"read-{accountId:N}@example.com",
            Email = $"read-{accountId:N}@example.com"
        });
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
        Assert.NotEqual(accountId, person.Id);
        return accountId;
    }

    private async Task SaveAsync(params object[] entities)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        db.AddRange(entities);
        await db.SaveChangesAsync();
    }

    private static HttpRequestMessage Request(string path, Guid accountId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(TestAuthenticationHandler.AccountIdHeaderName, accountId.ToString());
        return request;
    }

    private static DateTime ToPostgresPrecision(DateTime value)
        => value.AddTicks(-(value.Ticks % TimeSpan.TicksPerMicrosecond));

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private sealed class ReadCounter : DbCommandInterceptor
    {
        public int Count { get; set; }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return ValueTask.FromResult(result);
        }
    }
}
