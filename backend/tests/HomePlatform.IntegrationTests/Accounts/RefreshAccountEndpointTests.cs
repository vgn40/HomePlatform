using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HomePlatform.Api.Households;
using HomePlatform.Application.Accounts.Refresh;
using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Identity;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests.Accounts;

public sealed class RefreshAccountEndpointTests : IAsyncLifetime
{
    private const string Password = "ValidPassword123!";

    // Keep the same per-case PostgreSQL isolation as the existing account tests.
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
            _postgres.GetConnectionString(), useTestAuthentication: false);

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        _client = Factory.CreateClient();
    }

    [Fact]
    public async Task Refresh_issues_new_tokens_and_authenticates_household_with_registered_account()
    {
        await AssertRefreshAndHouseholdAsync(Client);
    }

    [Fact]
    public async Task Refresh_remains_anonymous_under_authenticated_fallback_policy()
    {
        await using var factory = new HomePlatformApiFactory(
            _postgres.GetConnectionString(),
            services => services.AddAuthorization(options =>
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build()),
            useTestAuthentication: false);
        using var client = factory.CreateClient();

        // /health has no explicit authorization, so this proves fallback is active.
        using var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.Unauthorized, health.StatusCode);
        await AssertRefreshAndHouseholdAsync(client);
    }

    [Fact]
    public async Task Refresh_returns_exactly_one_framework_token_response()
    {
        var (_, tokens) = await RegisterAndSignInAsync(Client);

        using var response = await Client.PostAsJsonAsync("/api/accounts/refresh",
            new { refreshToken = tokens.RefreshToken });

        await ReadTokenResponseAsync(response);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"refreshToken\":null}")]
    [InlineData("{\"refreshToken\":\"\"}")]
    [InlineData("{\"refreshToken\":\"   \"}")]
    public async Task Missing_or_blank_refresh_token_returns_required_public_error(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await Client.PostAsync("/api/accounts/refresh", content);

        await AssertPublicProblemAsync(response, HttpStatusCode.BadRequest,
            AccountRefreshErrorCode.RefreshTokenRequired);
    }

    [Fact]
    public async Task Malformed_refresh_token_returns_only_InvalidRefreshToken()
    {
        const string token = "not-a-real-refresh-token!";
        using var response = await Client.PostAsJsonAsync("/api/accounts/refresh",
            new { refreshToken = token });

        await AssertPublicProblemAsync(response, HttpStatusCode.Unauthorized,
            AccountRefreshErrorCode.InvalidRefreshToken, token);
    }

    [Fact]
    public async Task Tampered_refresh_token_returns_only_InvalidRefreshToken()
    {
        var (_, tokens) = await RegisterAndSignInAsync(Client);
        var characters = tokens.RefreshToken.ToCharArray();
        var middle = characters.Length / 2;
        characters[middle] = characters[middle] == 'A' ? 'B' : 'A';
        var tamperedToken = new string(characters);
        Assert.NotEqual(tokens.RefreshToken, tamperedToken);

        using var response = await Client.PostAsJsonAsync("/api/accounts/refresh",
            new { refreshToken = tamperedToken });

        await AssertPublicProblemAsync(response, HttpStatusCode.Unauthorized,
            AccountRefreshErrorCode.InvalidRefreshToken,
            tamperedToken, tokens.RefreshToken, tokens.AccessToken);
    }

    [Fact]
    public async Task Changed_security_stamp_invalidates_existing_refresh_token()
    {
        var (accountId, tokens) = await RegisterAndSignInAsync(Client);
        string oldStamp;
        string newStamp;
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await manager.FindByIdAsync(accountId.ToString());
            Assert.NotNull(user);
            oldStamp = await manager.GetSecurityStampAsync(user);
            Assert.True((await manager.UpdateSecurityStampAsync(user)).Succeeded);
            newStamp = await manager.GetSecurityStampAsync(user);
            Assert.NotEqual(oldStamp, newStamp);
        }

        using var response = await Client.PostAsJsonAsync("/api/accounts/refresh",
            new { refreshToken = tokens.RefreshToken });

        await AssertPublicProblemAsync(response, HttpStatusCode.Unauthorized,
            AccountRefreshErrorCode.InvalidRefreshToken,
            tokens.RefreshToken, tokens.AccessToken, oldStamp, newStamp);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Refresh_enforces_expiry_boundary_without_waiting(int secondsFromExpiry)
    {
        var clock = new ControllableTimeProvider();
        await using var factory = new HomePlatformApiFactory(
            _postgres.GetConnectionString(),
            services =>
            {
                // The production adapter and framework issuer must observe the same clock.
                services.AddSingleton<TimeProvider>(clock);
                services.PostConfigure<BearerTokenOptions>(IdentityConstants.BearerScheme,
                    options => options.TimeProvider = clock);
            },
            useTestAuthentication: false);
        using var client = factory.CreateClient();
        var options = factory.Services.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>()
            .Get(IdentityConstants.BearerScheme);
        var issuedAt = clock.GetUtcNow();
        var (_, tokens) = await RegisterAndSignInAsync(client);

        clock.UtcNow = issuedAt + options.RefreshTokenExpiration
            + TimeSpan.FromSeconds(secondsFromExpiry);
        using var response = await client.PostAsJsonAsync("/api/accounts/refresh",
            new { refreshToken = tokens.RefreshToken });

        if (secondsFromExpiry < 0)
        {
            // A valid token just before expiry guards against an always-rejecting setup.
            await ReadTokenResponseAsync(response);
        }
        else
        {
            await AssertPublicProblemAsync(response, HttpStatusCode.Unauthorized,
                AccountRefreshErrorCode.InvalidRefreshToken, tokens.RefreshToken, tokens.AccessToken);
        }
    }

    private async Task AssertRefreshAndHouseholdAsync(HttpClient client)
    {
        var (accountId, original) = await RegisterAndSignInAsync(client);

        using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/accounts/refresh")
        {
            Content = JsonContent.Create(new { refreshToken = original.RefreshToken })
        };
        Assert.Null(client.DefaultRequestHeaders.Authorization);
        Assert.Null(refreshRequest.Headers.Authorization);
        using var refreshResponse = await client.SendAsync(refreshRequest);
        var refreshed = await ReadTokenResponseAsync(refreshResponse);
        Assert.NotEqual(original.AccessToken, refreshed.AccessToken);
        Assert.NotEqual(original.RefreshToken, refreshed.RefreshToken);

        using var anonymous = await client.PostAsJsonAsync("/api/households", new { name = "Mit hjem" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using var householdRequest = new HttpRequestMessage(HttpMethod.Post, "/api/households")
        {
            Content = JsonContent.Create(new { name = "Mit hjem" })
        };
        householdRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.AccessToken);
        using var response = await client.SendAsync(householdRequest);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateHouseholdResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.HouseholdId);

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        var household = await dbContext.Set<Household>().AsNoTracking()
            .Include(candidate => candidate.Members).SingleAsync();
        Assert.Equal(body.HouseholdId, household.Id);
        Assert.Equal("Mit hjem", household.Name);
        var owner = Assert.Single(household.Members);
        Assert.Equal(HouseholdRole.Owner, owner.Role);
        Assert.Equal((await dbContext.Users.SingleAsync(user => user.Id == accountId)).PersonId, owner.PersonId);
        Assert.NotEqual(accountId, owner.PersonId);
    }

    private static async Task<(Guid AccountId, AccessTokenResponse Tokens)> RegisterAndSignInAsync(HttpClient client)
    {
        var credentials = new { email = $"refresh-{Guid.NewGuid():N}@example.com", password = Password };
        using var registration = await client.PostAsJsonAsync("/api/accounts/register", credentials);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        var body = await registration.Content.ReadFromJsonAsync<JsonElement>();
        var accountId = body.GetProperty("accountId").GetGuid();
        Assert.NotEqual(Guid.Empty, accountId);
        using var signIn = await client.PostAsJsonAsync("/api/accounts/sign-in", credentials);
        return (accountId, await ReadTokenResponseAsync(signIn));
    }

    private static async Task<AccessTokenResponse> ReadTokenResponseAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadAsStringAsync();
        // Parse the complete body: appended JSON or text must fail, even after a valid response.
        using var document = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Object, document.RootElement.ValueKind);
        Assert.Equal(new[] { "accessToken", "expiresIn", "refreshToken", "tokenType" },
            document.RootElement.EnumerateObject().Select(property => property.Name).Order());
        var tokens = document.RootElement.Deserialize<AccessTokenResponse>(JsonSerializerOptions.Web);
        Assert.NotNull(tokens);
        Assert.Equal("Bearer", document.RootElement.GetProperty("tokenType").GetString());
        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));
        Assert.True(tokens.ExpiresIn > 0);
        return tokens;
    }

    private static async Task AssertPublicProblemAsync(
        HttpResponseMessage response, HttpStatusCode status, AccountRefreshErrorCode expectedError,
        params string[] secrets)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var body = document.RootElement;
        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.Equal(status == HttpStatusCode.BadRequest
            ? "Refresh request is invalid." : "Refresh failed.", body.GetProperty("title").GetString());
        Assert.Equal(new[] { expectedError.ToString() }, body.GetProperty("errors").Deserialize<string[]>());
        Assert.All(body.EnumerateObject(), property =>
            Assert.Contains(property.Name, new[] { "type", "title", "status", "errors", "traceId" }));
        Assert.True(Uri.TryCreate(body.GetProperty("type").GetString(), UriKind.Absolute, out _));

        foreach (var detail in secrets.Concat(new[]
        {
            "decrypt", "unprotect", "Identity", "StackTrace", " at Microsoft.", "Exception",
            "SecurityStamp", "security stamp", "\"accessToken\"", "\"refreshToken\"", "AspNetUsers"
        }))
        {
            Assert.DoesNotContain(detail, json, StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class ControllableTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => UtcNow;
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

    private HomePlatformApiFactory Factory =>
        _factory ?? throw new InvalidOperationException("Test fixture is not initialized.");

    private HttpClient Client =>
        _client ?? throw new InvalidOperationException("Test fixture is not initialized.");
}
