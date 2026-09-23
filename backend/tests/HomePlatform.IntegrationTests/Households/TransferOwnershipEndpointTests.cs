using System.Net;
using System.Net.Http.Json;
using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests.Households;

public sealed class TransferOwnershipEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine")
            .WithDatabase("homeplatform")
            .WithUsername("homeplatform")
            .WithPassword("homeplatform-dev")
            .Build();

    private HomePlatformApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new HomePlatformApiFactory(
            _postgres.GetConnectionString());

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<HomePlatformDbContext>();

        await db.Database.MigrateAsync();

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Anonymous_request_returns_401_and_preserves_state()
    {
        var seed = await SeedAsync();

        await AssertRejectedAsync(
            seed,
            null,
            seed.HouseholdId.ToString(),
            new
            {
                newOwnerMembershipId = seed.TargetId
            },
            HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Nonexistent_household_returns_404_and_preserves_state()
    {
        var seed = await SeedAsync();

        await AssertRejectedAsync(
            seed,
            seed.OwnerAccountId,
            Guid.NewGuid().ToString(),
            new
            {
                newOwnerMembershipId = seed.TargetId
            },
            HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Non_owner_returns_403_and_preserves_state(
        HouseholdRole callerRole)
    {
        var seed =
            await SeedAsync(
                callerRole: callerRole);

        await AssertRejectedAsync(
            seed,
            seed.OtherAccountId,
            seed.HouseholdId.ToString(),
            new
            {
                newOwnerMembershipId = seed.TargetId
            },
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Non_member_returns_404_and_preserves_state()
    {
        var seed = await SeedAsync();

        var outsider = Guid.NewGuid();

        await _factory.CreateIdentityAccountAsync(
            outsider);

        await AssertRejectedAsync(
            seed,
            outsider,
            seed.HouseholdId.ToString(),
            new
            {
                newOwnerMembershipId = seed.TargetId
            },
            HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Non_member_with_invalid_target_returns_404_and_preserves_state()
    {
        var seed = await SeedAsync();

        var outsider = Guid.NewGuid();

        await _factory.CreateIdentityAccountAsync(
            outsider);

        await AssertRejectedAsync(
            seed,
            outsider,
            seed.HouseholdId.ToString(),
            new
            {
                newOwnerMembershipId = Guid.NewGuid()
            },
            HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(
        "missing",
        "New owner is not a member of this household.")]
    [InlineData(
        "loginless",
        "New owner must be linked to an account.")]
    [InlineData(
        "self",
        "Owner cannot transfer ownership to themselves.")]
    public async Task Invalid_target_returns_400_problem_and_preserves_state(
        string targetKind,
        string expectedDetail)
    {
        var seed = await SeedAsync();

        var targetId = targetKind switch
        {
            "loginless" => seed.LoginlessId,
            "self" => seed.OwnerId,
            _ => Guid.NewGuid()
        };

        await AssertRejectedAsync(
            seed,
            seed.OwnerAccountId,
            seed.HouseholdId.ToString(),
            new
            {
                newOwnerMembershipId = targetId
            },
            HttpStatusCode.BadRequest,
            expectedDetail);
    }

    [Theory]
    [InlineData(HouseholdRole.Member)]
    [InlineData(HouseholdRole.Guest)]
    public async Task Successful_transfer_returns_empty_204_and_persists_only_role_changes(
        HouseholdRole targetRole)
    {
        var seed =
            await SeedAsync(targetRole);

        await AssertSuccessfulAsync(
            seed,
            new
            {
                newOwnerMembershipId = seed.TargetId
            });
    }

    [Fact]
    public async Task Request_body_cannot_impersonate_owner()
    {
        var seed = await SeedAsync();

        await AssertRejectedAsync(
            seed,
            seed.OtherAccountId,
            seed.HouseholdId.ToString(),
            new
            {
                newOwnerMembershipId = seed.TargetId,
                accountId = seed.OwnerAccountId,
                currentOwnerAccountId = seed.OwnerAccountId,
                actingAccountId = seed.OwnerAccountId
            },
            HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Authenticated_owner_and_target_membership_are_authoritative()
    {
        var seed = await SeedAsync();

        await AssertSuccessfulAsync(
            seed,
            new
            {
                newOwnerMembershipId = seed.TargetId,
                accountId = seed.OtherAccountId,
                currentOwnerAccountId = seed.OtherAccountId,
                actingAccountId = seed.OtherAccountId,
                newOwnerAccountId = seed.OtherAccountId
            });
    }

    [Fact]
    public async Task Route_is_not_exposed_in_production()
    {
        var seed = await SeedAsync();

        var before =
            await LoadAsync();

        await using var factory =
            new ProductionApiFactory(
                _postgres.GetConnectionString());

        using var client =
            factory.CreateClient();

        using var request =
            CreateRequest(
                seed.OwnerAccountId,
                seed.HouseholdId.ToString(),
                new
                {
                    newOwnerMembershipId = seed.TargetId
                });

        using var response =
            await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);

        AssertUnchanged(
            before,
            await LoadAsync());
    }

    [Fact]
    public async Task Malformed_household_id_does_not_match_route_and_preserves_state()
    {
        var seed = await SeedAsync();

        await AssertRejectedAsync(
            seed,
            seed.OwnerAccountId,
            "not-a-guid",
            new
            {
                newOwnerMembershipId = seed.TargetId
            },
            HttpStatusCode.NotFound);
    }

    private async Task AssertRejectedAsync(
        Seed seed,
        Guid? accountId,
        string householdId,
        object body,
        HttpStatusCode expectedStatus,
        string? expectedDetail = null)
    {
        var before =
            await LoadAsync();

        Assert.Equal(
            seed.HouseholdId,
            Assert.Single(before).Id);

        using var request =
            CreateRequest(
                accountId,
                householdId,
                body);

        using var response =
            await _client.SendAsync(request);

        Assert.Equal(
            expectedStatus,
            response.StatusCode);

        if (expectedDetail is not null)
        {
            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);

            var problem =
                await response.Content
                    .ReadFromJsonAsync<ProblemDetails>();

            Assert.NotNull(problem);

            Assert.Equal(
                400,
                problem.Status);

            Assert.Equal(
                "Invalid ownership transfer",
                problem.Title);

            Assert.Equal(
                expectedDetail,
                problem.Detail);
        }

        AssertUnchanged(
            before,
            await LoadAsync());
    }

    private async Task AssertSuccessfulAsync(
        Seed seed,
        object body)
    {
        var before =
            Assert.Single(
                await LoadAsync());

        using var request =
            CreateRequest(
                seed.OwnerAccountId,
                seed.HouseholdId.ToString(),
                body);

        using var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        Assert.Empty(
            await response.Content
                .ReadAsByteArrayAsync());

        var after =
            Assert.Single(
                await LoadAsync());

        Assert.Equal(
            before.Id,
            after.Id);

        Assert.Equal(
            before.Name,
            after.Name);

        Assert.Equal(
            before.CreatedAt,
            after.CreatedAt);

        Assert.Equal(
            before.Members.Count,
            after.Members.Count);

        foreach (var original in before.Members)
        {
            var member =
                Assert.Single(
                    after.Members,
                    candidate =>
                        candidate.MembershipId ==
                        original.MembershipId);

            Assert.Equal(
                original.PersonId,
                member.PersonId);

            var expectedRole =
                original.MembershipId == seed.TargetId
                    ? HouseholdRole.Owner
                    : original.MembershipId == seed.OwnerId
                        ? HouseholdRole.Member
                        : original.Role;

            Assert.Equal(
                expectedRole,
                member.Role);
        }

        Assert.Equal(
            seed.TargetId,
            Assert.Single(
                after.Members,
                member =>
                    member.Role ==
                    HouseholdRole.Owner)
                .MembershipId);
    }

    private async Task<Seed> SeedAsync(
        HouseholdRole targetRole = HouseholdRole.Member,
        HouseholdRole callerRole = HouseholdRole.Member)
    {
        var loginlessPersonId = await _factory!.CreateLoginlessPersonAsync();
        var ownerAccountId =
            Guid.NewGuid();

        var targetAccountId =
            Guid.NewGuid();

        var otherAccountId =
            Guid.NewGuid();

        await _factory.CreateIdentityAccountAsync(
            ownerAccountId);

        await _factory.CreateIdentityAccountAsync(
            targetAccountId);

        await _factory.CreateIdentityAccountAsync(
            otherAccountId);

        var household =
            new Household(
                "Ownership contract",
                ownerAccountId);

        Assert.True(
            household.AddMember(
                household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
                targetRole,
                targetAccountId).IsSuccess);

        Assert.True(
            household.AddMember(
                household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
                callerRole,
                otherAccountId).IsSuccess);

        Assert.True(
            household.AddMember(
                household.Members.Single(member => member.Role == HouseholdRole.Owner).PersonId,
                HouseholdRole.Member, loginlessPersonId).IsSuccess);

        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<HomePlatformDbContext>();

        db.Add(household);

        await db.SaveChangesAsync();

        return new Seed(
            household.Id,
            ownerAccountId,
            otherAccountId,
            household.Members
                .Single(
                    member =>
                        member.PersonId ==
                        ownerAccountId)
                .MembershipId,
            household.Members
                .Single(
                    member =>
                        member.PersonId ==
                        targetAccountId)
                .MembershipId,
            household.Members
                .Single(
                    member =>
                        member.PersonId == loginlessPersonId)
                .MembershipId);
    }

    private async Task<List<Household>> LoadAsync()
    {
        await using var scope =
            _factory.Services.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<HomePlatformDbContext>();

        return await db.Set<Household>()
            .AsNoTracking()
            .Include(
                household =>
                    household.Members)
            .OrderBy(
                household =>
                    household.Id)
            .ToListAsync();
    }

    private static void AssertUnchanged(
        List<Household> before,
        List<Household> after)
    {
        Assert.Equal(
            before.Count,
            after.Count);

        foreach (var original in before)
        {
            var current =
                Assert.Single(
                    after,
                    household =>
                        household.Id ==
                        original.Id);

            Assert.Equal(
                original.Name,
                current.Name);

            Assert.Equal(
                original.CreatedAt,
                current.CreatedAt);

            Assert.Equal(
                original.UpdatedAt,
                current.UpdatedAt);

            Assert.Equal(
                original.Members
                    .OrderBy(
                        member =>
                            member.MembershipId)
                    .Select(
                        member =>
                            (
                                member.MembershipId,
                                member.PersonId,
                                member.Role))
                    .ToArray(),
                current.Members
                    .OrderBy(
                        member =>
                            member.MembershipId)
                    .Select(
                        member =>
                            (
                                member.MembershipId,
                                member.PersonId,
                                member.Role))
                    .ToArray());
        }
    }

    private static HttpRequestMessage CreateRequest(
        Guid? accountId,
        string householdId,
        object body)
    {
        var request =
            new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/households/{householdId}/ownership")
            {
                Content =
                    JsonContent.Create(body)
            };

        if (accountId is not null)
        {
            request.Headers.Add(
                TestAuthenticationHandler.AccountIdHeaderName,
                accountId.ToString());
        }

        return request;
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    private sealed record Seed(
        Guid HouseholdId,
        Guid OwnerAccountId,
        Guid OtherAccountId,
        Guid OwnerId,
        Guid TargetId,
        Guid LoginlessId);

    private sealed class ProductionApiFactory(
        string connectionString)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment(
                "Production");

            builder.UseSetting(
                "ConnectionStrings:Database",
                connectionString);
        }
    }
}
