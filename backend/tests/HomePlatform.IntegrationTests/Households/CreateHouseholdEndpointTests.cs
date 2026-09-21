using System.Net;
using System.Net.Http.Json;
using HomePlatform.Api.Households;
using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests.Households;

public sealed class CreateHouseholdEndpointTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine")
            .WithDatabase("homeplatform")
            .WithUsername("homeplatform")
            .WithPassword("homeplatform-dev")
            .Build();

    private HomePlatformApiFactory? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new HomePlatformApiFactory(
            _postgres.GetConnectionString());

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<HomePlatformDbContext>();

            await dbContext.Database.MigrateAsync();
        }

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Create_household_returns_401_when_anonymous()
    {
        await ResetDatabaseAsync();

        var response = await Client.PostAsJsonAsync(
            "/api/households",
            new { name = "Mit hjem" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertRowCountsAsync(
            expectedHouseholds: 0,
            expectedMembers: 0);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Create_household_returns_401_for_invalid_test_account_header(
        string accountIdHeader)
    {
        await ResetDatabaseAsync();

        using var request = CreateRequest(
            accountIdHeader,
            body: new { name = "Mit hjem" });

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertRowCountsAsync(
            expectedHouseholds: 0,
            expectedMembers: 0);
    }

    [Fact]
    public async Task Create_household_returns_201_and_persists_authenticated_actor_as_owner()
    {
        await ResetDatabaseAsync();

        var authenticatedAccountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(authenticatedAccountId);

        using var request = CreateRequest(
            authenticatedAccountId.ToString(),
            new { name = "Mit hjem" });

        var response = await Client.SendAsync(request);
        var body = await response.Content
            .ReadFromJsonAsync<CreateHouseholdResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.HouseholdId);
        Assert.Equal("Mit hjem", body.Name);
        Assert.Equal(
            $"/api/households/{body.HouseholdId}",
            response.Headers.Location?.OriginalString);

        await AssertPersistedOwnerAsync(
            body.HouseholdId,
            authenticatedAccountId);
    }

    [Fact]
    public async Task Create_household_uses_authenticated_actor_not_request_account_id()
    {
        await ResetDatabaseAsync();

        var authenticatedAccountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(authenticatedAccountId);
        var callerSelectedAccountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(callerSelectedAccountId);

        using var request = CreateRequest(
            authenticatedAccountId.ToString(),
            new
            {
                name = "Mit hjem",
                accountId = callerSelectedAccountId
            });

        var response = await Client.SendAsync(request);
        var body = await response.Content
            .ReadFromJsonAsync<CreateHouseholdResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);

        await AssertPersistedOwnerAsync(
            body.HouseholdId,
            authenticatedAccountId);

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<HomePlatformDbContext>();

        Assert.DoesNotContain(
            await dbContext.Set<HouseholdMember>().ToListAsync(),
            member => member.PersonId == callerSelectedAccountId);
    }

    [Fact]
    public async Task Create_household_returns_400_for_invalid_name_and_writes_nothing()
    {
        await ResetDatabaseAsync();

        var authenticatedAccountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(authenticatedAccountId);
        using var request = CreateRequest(
            authenticatedAccountId.ToString(),
            new { name = "   " });

        var response = await Client.SendAsync(request);
        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Invalid household", problem.Title);
        await AssertRowCountsAsync(
            expectedHouseholds: 0,
            expectedMembers: 0);
    }

    [Fact]
    public async Task Create_household_returns_400_for_name_exceeding_max_length_and_writes_nothing()
    {
        await ResetDatabaseAsync();

        var authenticatedAccountId = Guid.NewGuid();
        await Factory.CreateIdentityAccountAsync(authenticatedAccountId);
        var name = new string('a', Household.MaxNameLength + 1);
        using var request = CreateRequest(
            authenticatedAccountId.ToString(),
            new { name });

        var response = await Client.SendAsync(request);
        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal(StatusCodes.Status400BadRequest, problem.Status);
        Assert.Equal("Invalid household", problem.Title);
        await AssertRowCountsAsync(
            expectedHouseholds: 0,
            expectedMembers: 0);
    }

    [Fact]
    public async Task Create_household_route_is_not_exposed_in_production()
    {
        await ResetDatabaseAsync();

        await using var factory = new ProductionApiFactory(
            _postgres.GetConnectionString());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/households",
            new { name = "Mit hjem" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertRowCountsAsync(
            expectedHouseholds: 0,
            expectedMembers: 0);
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

    private HttpClient Client =>
        _client
        ?? throw new InvalidOperationException(
            "Test fixture is not initialized.");

    private HomePlatformApiFactory Factory =>
        _factory
        ?? throw new InvalidOperationException(
            "Test fixture is not initialized.");

    private static HttpRequestMessage CreateRequest(
        string accountIdHeader,
        object body)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/households")
        {
            Content = JsonContent.Create(body)
        };

        request.Headers.Add(
            TestAuthenticationHandler.AccountIdHeaderName,
            accountIdHeader);

        return request;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<HomePlatformDbContext>();

        await dbContext.Set<Household>().ExecuteDeleteAsync();
    }

    private async Task AssertRowCountsAsync(
        int expectedHouseholds,
        int expectedMembers)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<HomePlatformDbContext>();

        Assert.Equal(
            expectedHouseholds,
            await dbContext.Set<Household>().CountAsync());

        Assert.Equal(
            expectedMembers,
            await dbContext.Set<HouseholdMember>().CountAsync());
    }

    private async Task AssertPersistedOwnerAsync(
        Guid expectedHouseholdId,
        Guid expectedOwnerAccountId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<HomePlatformDbContext>();

        var household = await dbContext.Set<Household>()
            .Include(candidate => candidate.Members)
            .SingleAsync();

        Assert.Equal(expectedHouseholdId, household.Id);
        Assert.Equal("Mit hjem", household.Name);

        var owner = Assert.Single(household.Members);
        Assert.NotEqual(Guid.Empty, owner.MembershipId);
        Assert.Equal(HouseholdRole.Owner, owner.Role);
        Assert.Equal(expectedOwnerAccountId, owner.PersonId);

        await AssertRowCountsAsync(
            expectedHouseholds: 1,
            expectedMembers: 1);
    }

    private sealed class ProductionApiFactory(
        string connectionString)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting(
                "ConnectionStrings:Database",
                connectionString);
        }
    }
}
