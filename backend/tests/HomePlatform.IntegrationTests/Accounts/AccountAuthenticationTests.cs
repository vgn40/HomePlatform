using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HomePlatform.Api.Households;
using HomePlatform.Application.Accounts.SignIn;
using HomePlatform.Domain.Household;
using HomePlatform.Infrastructure.Identity;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests.Accounts;

public sealed class AccountAuthenticationTests : IAsyncLifetime
{
    private const string Password = "ValidPassword123!";

    // xUnit creates a fresh instance/container for every test case, as in registration tests.
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
            var dbContext = scope.ServiceProvider
                .GetRequiredService<HomePlatformDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        _client = Factory.CreateClient();
    }

    [Fact]
    public async Task Sign_in_returns_bearer_access_and_refresh_tokens()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        await SignInAndReadAccessTokenAsync(email);
    }

    [Fact]
    public async Task Access_token_authenticates_household_request_and_persists_registered_account_as_owner()
    {
        var email = NewEmail();
        var accountId = await RegisterAsync(email);
        var accessToken = await SignInAndReadAccessTokenAsync(email);

        // Signing in must not authenticate subsequent requests without the bearer header.
        using var anonymousResponse = await Client.PostAsJsonAsync(
            "/api/households", new { name = "Mit hjem" });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/households")
        {
            Content = JsonContent.Create(new { name = "Mit hjem" })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateHouseholdResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.HouseholdId);

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        var household = await dbContext.Set<Household>()
            .AsNoTracking()
            .Include(candidate => candidate.Members)
            .SingleAsync();
        Assert.Equal(body.HouseholdId, household.Id);
        Assert.Equal("Mit hjem", household.Name);
        var owner = Assert.Single(household.Members);
        Assert.Equal(HouseholdRole.Owner, owner.Role);
        Assert.Equal(accountId, owner.AccountId);
    }

    [Theory]
    [InlineData("{\"password\":\"ValidPassword123!\"}")]
    [InlineData("{\"email\":null,\"password\":\"ValidPassword123!\"}")]
    [InlineData("{\"email\":\"\",\"password\":\"ValidPassword123!\"}")]
    [InlineData("{\"email\":\"   \",\"password\":\"ValidPassword123!\"}")]
    public async Task Sign_in_returns_EmailRequired_for_missing_or_blank_email(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await Client.PostAsync("/api/accounts/sign-in", content);
        await AssertPublicProblemAsync(response, HttpStatusCode.BadRequest,
            nameof(AccountAuthenticationErrorCode.EmailRequired));
    }

    [Theory]
    [InlineData("{\"email\":\"validation@example.com\"}")]
    [InlineData("{\"email\":\"validation@example.com\",\"password\":null}")]
    [InlineData("{\"email\":\"validation@example.com\",\"password\":\"\"}")]
    [InlineData("{\"email\":\"validation@example.com\",\"password\":\"   \"}")]
    public async Task Sign_in_returns_PasswordRequired_for_missing_or_blank_password(string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await Client.PostAsync("/api/accounts/sign-in", content);
        await AssertPublicProblemAsync(response, HttpStatusCode.BadRequest,
            nameof(AccountAuthenticationErrorCode.PasswordRequired));
    }

    [Fact]
    public async Task Sign_in_returns_both_required_errors_for_empty_request()
    {
        using var response = await Client.PostAsJsonAsync("/api/accounts/sign-in", new { });
        await AssertPublicProblemAsync(response, HttpStatusCode.BadRequest,
            nameof(AccountAuthenticationErrorCode.EmailRequired),
            nameof(AccountAuthenticationErrorCode.PasswordRequired));
    }

    [Fact]
    public async Task Sign_in_returns_only_InvalidCredentials_for_unknown_email()
    {
        using var response = await Client.PostAsJsonAsync("/api/accounts/sign-in",
            new { email = NewEmail(), password = Password });
        await AssertPublicProblemAsync(response, HttpStatusCode.Unauthorized,
            nameof(AccountAuthenticationErrorCode.InvalidCredentials));
    }

    [Fact]
    public async Task Sign_in_returns_only_InvalidCredentials_for_wrong_password_and_increments_failed_count()
    {
        var email = NewEmail();
        var accountId = await RegisterAsync(email);
        var before = await ReadUserAsync(accountId);
        Assert.True(before.LockoutEnabled);
        Assert.Equal(0, before.AccessFailedCount);

        using var response = await Client.PostAsJsonAsync("/api/accounts/sign-in",
            new { email, password = "WrongPassword123!" });
        await AssertPublicProblemAsync(response, HttpStatusCode.Unauthorized,
            nameof(AccountAuthenticationErrorCode.InvalidCredentials));

        var after = await ReadUserAsync(accountId);
        Assert.Equal(before.AccessFailedCount + 1, after.AccessFailedCount);
    }

    [Fact]
    public async Task Sign_in_returns_only_InvalidCredentials_for_locked_account_with_correct_password()
    {
        var email = NewEmail();
        var accountId = await RegisterAsync(email);
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(accountId.ToString());
            Assert.NotNull(user);
            Assert.True((await userManager.SetLockoutEnabledAsync(user, true)).Succeeded);
            Assert.True((await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue)).Succeeded);
            Assert.True(await userManager.IsLockedOutAsync(user));
        }

        using var response = await Client.PostAsJsonAsync("/api/accounts/sign-in",
            new { email, password = Password });
        await AssertPublicProblemAsync(response, HttpStatusCode.Unauthorized,
            nameof(AccountAuthenticationErrorCode.InvalidCredentials));
    }

    private async Task<Guid> RegisterAsync(string email)
    {
        using var response = await Client.PostAsJsonAsync("/api/accounts/register",
            new { email, password = Password });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var accountId = body.GetProperty("accountId").GetGuid();
        Assert.NotEqual(Guid.Empty, accountId);
        return accountId;
    }

    private async Task<string> SignInAndReadAccessTokenAsync(string email)
    {
        using var response = await Client.PostAsJsonAsync("/api/accounts/sign-in",
            new { email, password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = body.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("refreshToken").GetString()));
        Assert.Equal("Bearer", body.GetProperty("tokenType").GetString());
        Assert.True(body.GetProperty("expiresIn").GetInt64() > 0);
        return accessToken!;
    }

    private static async Task AssertPublicProblemAsync(
        HttpResponseMessage response, HttpStatusCode status, params string[] expectedErrors)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)status, body.GetProperty("status").GetInt32());
        Assert.Equal(status == HttpStatusCode.BadRequest
            ? "Sign-in request is invalid." : "Sign-in failed.", body.GetProperty("title").GetString());
        Assert.Equal(expectedErrors.Order(), body.GetProperty("errors").Deserialize<string[]>()!.Order());

        // Only public ProblemDetails fields and the standard trace ID are allowed: no Identity descriptions,
        // account-state extensions, tokens or other internal details, even outside errors.
        Assert.All(body.EnumerateObject(), property =>
            Assert.Contains(property.Name, new[] { "type", "title", "status", "errors", "traceId" }));
        Assert.True(Uri.TryCreate(body.GetProperty("type").GetString(), UriKind.Absolute, out _));
    }

    private async Task<ApplicationUser> ReadUserAsync(Guid accountId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        return await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == accountId);
    }

    private static string NewEmail() => $"auth-{Guid.NewGuid():N}@example.com";

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
