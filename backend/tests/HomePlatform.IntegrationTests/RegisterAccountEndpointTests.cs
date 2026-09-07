using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HomePlatform.Application.Accounts;
using HomePlatform.Infrastructure.Identity;
using HomePlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests;

public sealed class RegisterAccountEndpointTests : IAsyncLifetime
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

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<HomePlatformDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        _client = Factory.CreateClient();
    }

    [Fact]
    public async Task Register_account_returns_201_and_persists_email_and_password_hash()
    {
        const string email = "victor@example.com";
        const string password = "ValidPassword123!";

        using var response = await Client.PostAsJsonAsync(
            "/api/accounts/register", new { email, password });

        var accountId = await AssertCreatedAsync(response);

        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<HomePlatformDbContext>();
        var user = Assert.Single(await dbContext.Users.AsNoTracking().ToListAsync());

        Assert.Equal(accountId, user.Id);
        Assert.Equal(email, user.Email);
        Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
        Assert.NotEqual(password, user.PasswordHash);

        var hasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher<ApplicationUser>>();
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(user, user.PasswordHash!, password));

        // Inspect every persisted user column, including fields beyond PasswordHash.
        var persistedRows = await dbContext.Database.SqlQueryRaw<string>(
            """SELECT row_to_json(u)::text AS "Value" FROM "AspNetUsers" AS u""")
            .ToListAsync();
        Assert.DoesNotContain(password, Assert.Single(persistedRows));
    }

    [Fact]
    public async Task Register_account_returns_400_for_duplicate_email_and_keeps_one_user()
    {
        const string email = "duplicate@example.com";
        var payload = new { email, password = "ValidPassword123!" };

        using var firstResponse = await Client.PostAsJsonAsync(
            "/api/accounts/register", payload);
        var accountId = await AssertCreatedAsync(firstResponse);

        using var secondResponse = await Client.PostAsJsonAsync(
            "/api/accounts/register", payload);
        var errors = await ReadErrorsAsync(secondResponse);

        await using var scope = Factory.Services.CreateAsyncScope();
        Assert.Equal([nameof(AccountRegistrationErrorCode.EmailAlreadyExists)], errors);

        var dbContext = scope.ServiceProvider
            .GetRequiredService<HomePlatformDbContext>();
        var user = Assert.Single(await dbContext.Users.AsNoTracking().ToListAsync());
        Assert.Equal(accountId, user.Id);
        Assert.Equal(email, user.Email);
    }

    [Fact]
    public async Task Register_account_returns_400_for_invalid_password_and_writes_nothing()
    {
        // AddIdentityCore uses the default password policy, including a required digit.
        using var response = await Client.PostAsJsonAsync(
            "/api/accounts/register",
            new { email = "invalid@example.com", password = "a" });
        var errors = await ReadErrorsAsync(response);

        await using var scope = Factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        Assert.True(userManager.Options.Password.RequireDigit);
        Assert.Equal(
            new[]
            {
                nameof(AccountRegistrationErrorCode.PasswordTooShort),
                nameof(AccountRegistrationErrorCode.PasswordRequiresDigit),
                nameof(AccountRegistrationErrorCode.PasswordRequiresUppercase),
                nameof(AccountRegistrationErrorCode.PasswordRequiresNonAlphanumeric)
            }.Order(),
            errors.Order());
        var dbContext = scope.ServiceProvider
            .GetRequiredService<HomePlatformDbContext>();
        Assert.Empty(await dbContext.Users.AsNoTracking().ToListAsync());
    }

    [Theory]
    [InlineData("{\"password\":\"ValidPassword123!\"}")]
    [InlineData("{\"email\":null,\"password\":\"ValidPassword123!\"}")]
    public async Task Register_account_returns_400_for_missing_email_and_writes_nothing(
        string json)
    {
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await Client.PostAsync("/api/accounts/register", content);
        var errors = await ReadErrorsAsync(response);

        Assert.Equal([nameof(AccountRegistrationErrorCode.EmailRequired)], errors);
        await AssertNoUsersAsync();
    }

    [Fact]
    public async Task Register_account_returns_400_for_invalid_email_format_and_writes_nothing()
    {
        using var response = await Client.PostAsJsonAsync(
            "/api/accounts/register",
            new { email = "not-an-email", password = "ValidPassword123!" });
        var errors = await ReadErrorsAsync(response);

        Assert.Equal([nameof(AccountRegistrationErrorCode.EmailInvalid)], errors);
        await AssertNoUsersAsync();
    }

    [Fact]
    public async Task Register_account_returns_400_for_email_exceeding_max_length_and_writes_nothing()
    {
        // 255 characters: one beyond the public registration limit of 254.
        var email = new string('a', 64) + "@"
            + new string('b', 63) + "." + new string('c', 63) + "."
            + new string('d', 58) + ".com";

        using var response = await Client.PostAsJsonAsync(
            "/api/accounts/register",
            new { email, password = "ValidPassword123!" });
        var errors = await ReadErrorsAsync(response);

        Assert.Equal([nameof(AccountRegistrationErrorCode.EmailTooLong)], errors);
        await AssertNoUsersAsync();
    }

    [Theory]
    [InlineData("{\"email\":\"missing-password@example.com\"}")]
    [InlineData("{\"email\":\"missing-password@example.com\",\"password\":null}")]
    [InlineData("{\"email\":\"missing-password@example.com\",\"password\":\"\"}")]
    [InlineData("{\"email\":\"missing-password@example.com\",\"password\":\"   \"}")]
    public async Task Register_account_returns_400_for_missing_password_and_writes_nothing(
        string json)
    {
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await Client.PostAsync("/api/accounts/register", content);
        var errors = await ReadErrorsAsync(response);

        Assert.Equal([nameof(AccountRegistrationErrorCode.PasswordRequired)], errors);
        await AssertNoUsersAsync();
    }

    [Fact]
    public async Task Register_account_returns_201_when_anonymous_with_authorization_required_by_default()
    {
        await using var factory = new HomePlatformApiFactory(
            _postgres.GetConnectionString(),
            services => services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            }));
        using var client = factory.CreateClient();

        Assert.Null(client.DefaultRequestHeaders.Authorization);
        Assert.False(client.DefaultRequestHeaders.Contains(
            TestAuthenticationHandler.AccountIdHeaderName));

        // Control request proves that this client is anonymous in the HTTP pipeline.
        using var protectedResponse = await client.PostAsJsonAsync(
            "/api/households", new { name = "Mit hjem" });
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);

        using var response = await client.PostAsJsonAsync(
            "/api/accounts/register",
            new { email = "anonymous@example.com", password = "ValidPassword123!" });
        var accountId = await AssertCreatedAsync(response);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<HomePlatformDbContext>();
        Assert.Equal(accountId, (await dbContext.Users.SingleAsync()).Id);
    }

    [Fact]
    public async Task Register_account_maps_unknown_identity_errors_without_exposing_descriptions()
    {
        await using var factory = new HomePlatformApiFactory(
            _postgres.GetConnectionString(),
            services => services.AddScoped<IUserValidator<ApplicationUser>, RejectingUserValidator>());
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/accounts/register",
            new { email = "rejected@example.com", password = "ValidPassword123!" });

        Assert.Equal(
            [nameof(AccountRegistrationErrorCode.RegistrationFailed)],
            await ReadErrorsAsync(response));
        await AssertNoUsersAsync();
    }

    [Fact]
    public async Task Register_account_maps_password_requires_lowercase_and_writes_nothing()
    {
        using var response = await Client.PostAsJsonAsync(
            "/api/accounts/register",
            new { email = "uppercase@example.com", password = "VALIDPASSWORD123!" });

        Assert.Equal(
            [nameof(AccountRegistrationErrorCode.PasswordRequiresLowercase)],
            await ReadErrorsAsync(response));
        await AssertNoUsersAsync();
    }

    [Fact]
    public async Task Concurrent_duplicate_registration_returns_201_and_400_after_both_pass_identity_checks()
    {
        const string email = "race@example.com";
        var barrier = new RegistrationSaveBarrier(email);
        await using var factory = new HomePlatformApiFactory(
            _postgres.GetConnectionString(),
            services => services.AddDbContext<HomePlatformDbContext>(
                options => options.AddInterceptors(barrier)));
        using var client = factory.CreateClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var payload = new { email, password = "ValidPassword123!" };

        var requestA = client.PostAsJsonAsync("/api/accounts/register", payload, timeout.Token);
        await barrier.FirstArrived.Task.WaitAsync(timeout.Token);
        var requestB = client.PostAsJsonAsync("/api/accounts/register", payload, timeout.Token);
        await barrier.BothArrived.Task.WaitAsync(timeout.Token);

        // Both real UserManager calls have completed validation and staged an INSERT.
        // Neither SaveChanges may reach PostgreSQL until the test releases it.
        Assert.Equal(2, barrier.Arrivals);
        await AssertNoUsersAsync();
        barrier.ReleaseFirst.SetResult();
        using var responseA = await requestA;
        var accountId = await AssertCreatedAsync(responseA);
        barrier.ReleaseSecond.SetResult();
        using var responseB = await requestB;

        Assert.Equal(
            [nameof(AccountRegistrationErrorCode.EmailAlreadyExists)],
            await ReadErrorsAsync(responseB));
        Assert.Equal(1, barrier.SuccessfulSaves);
        var databaseFailure = Assert.IsType<DbUpdateException>(Assert.Single(barrier.Failures));
        var postgresFailure = Assert.IsType<PostgresException>(databaseFailure.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgresFailure.SqlState);
        Assert.Equal("UserNameIndex", postgresFailure.ConstraintName);
        await AssertNoInternalDetailsAsync(responseB, postgresFailure);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        var user = Assert.Single(await dbContext.Users.AsNoTracking().ToListAsync());
        Assert.Equal(accountId, user.Id);
        Assert.Equal(email, user.Email);
        Assert.Equal(email, user.UserName);
        Assert.Equal(email.ToUpperInvariant(), user.NormalizedEmail);
        Assert.Equal(email.ToUpperInvariant(), user.NormalizedUserName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Unrelated_database_registration_failure_returns_safe_500(bool uniqueViolation)
    {
        // Generate real provider failures: an unrelated unique index or a check constraint.
        // Neither is the Identity UserNameIndex duplicate handled by Infrastructure.
        if (uniqueViolation)
        {
            using var seed = await Client.PostAsJsonAsync("/api/accounts/register",
                new { email = "seed@example.com", password = "ValidPassword123!" });
            await AssertCreatedAsync(seed);
        }

        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync(uniqueViolation
                ? """CREATE UNIQUE INDEX "TestUnrelatedConstraint" ON "AspNetUsers" ((1))"""
                : """ALTER TABLE "AspNetUsers" ADD CONSTRAINT "TestUnrelatedConstraint" CHECK ("Email" <> 'failure@example.com')""");
        }

        var recorder = new SaveFailureRecorder();
        await using var factory = new HomePlatformApiFactory(
            _postgres.GetConnectionString(),
            services => services.AddDbContext<HomePlatformDbContext>(
                options => options.AddInterceptors(recorder)));
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/accounts/register",
            new { email = "failure@example.com", password = "ValidPassword123!" });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(500, body.GetProperty("status").GetInt32());
        Assert.False(body.TryGetProperty("errors", out _));
        var failure = Assert.IsType<DbUpdateException>(Assert.Single(recorder.Failures));
        var postgresFailure = Assert.IsType<PostgresException>(failure.InnerException);
        Assert.Equal(uniqueViolation ? PostgresErrorCodes.UniqueViolation : PostgresErrorCodes.CheckViolation,
            postgresFailure.SqlState);
        Assert.Equal("TestUnrelatedConstraint", postgresFailure.ConstraintName);
        await AssertNoInternalDetailsAsync(response, postgresFailure);

        await using var verificationScope = Factory.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        var users = await verificationContext.Users.AsNoTracking().ToListAsync();
        if (uniqueViolation)
        {
            Assert.Equal("seed@example.com", Assert.Single(users).Email);
        }
        else
        {
            Assert.Empty(users);
        }
    }

    private static async Task AssertNoInternalDetailsAsync(
        HttpResponseMessage response, PostgresException failure)
    {
        var body = await response.Content.ReadAsStringAsync();
        foreach (var detail in new[]
        {
            failure.ConstraintName!, failure.SqlState, failure.MessageText,
            "DbUpdateException", "PostgresException", "Npgsql", "StackTrace",
            "INSERT INTO", "ConnectionStrings", "homeplatform-dev"
        })
        {
            Assert.DoesNotContain(detail, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    private class SaveFailureRecorder : SaveChangesInterceptor
    {
        public System.Collections.Concurrent.ConcurrentQueue<Exception> Failures { get; } = new();

        public override Task SaveChangesFailedAsync(
            DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            Failures.Enqueue(eventData.Exception);
            return Task.CompletedTask;
        }
    }

    private sealed class RegistrationSaveBarrier(string email) : SaveFailureRecorder
    {
        private int _arrivals;
        private int _successfulSaves;
        public int Arrivals => Volatile.Read(ref _arrivals);
        public int SuccessfulSaves => Volatile.Read(ref _successfulSaves);
        public TaskCompletionSource FirstArrived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource BothArrived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseFirst { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseSecond { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var user = Assert.Single(eventData.Context!.ChangeTracker.Entries<ApplicationUser>());
            Assert.Equal(EntityState.Added, user.State);
            Assert.Equal(email, user.Entity.Email);
            var arrival = Interlocked.Increment(ref _arrivals);
            Assert.InRange(arrival, 1, 2);
            if (arrival == 1)
            {
                FirstArrived.SetResult();
            }
            else
            {
                BothArrived.SetResult();
            }

            // Timeout is only a deadlock guard, never the concurrency mechanism.
            await (arrival == 1 ? ReleaseFirst : ReleaseSecond).Task
                .WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            return result;
        }

        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _successfulSaves);
            return ValueTask.FromResult(result);
        }
    }

    private sealed class RejectingUserValidator : IUserValidator<ApplicationUser>
    {
        public Task<IdentityResult> ValidateAsync(
            UserManager<ApplicationUser> manager,
            ApplicationUser user) => Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "UnexpectedProviderError",
                Description = "Internal provider details must never leave Infrastructure."
            }));
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

    private async Task AssertNoUsersAsync()
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<HomePlatformDbContext>();
        Assert.Empty(await dbContext.Users.AsNoTracking().ToListAsync());
    }

    private static async Task<Guid> AssertCreatedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var accountId = body.GetProperty("accountId").GetGuid();
        Assert.NotEqual(Guid.Empty, accountId);
        Assert.Equal($"/api/accounts/{accountId}", response.Headers.Location?.OriginalString);
        return accountId;
    }

    private static async Task<string[]> ReadErrorsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        var errors = body.GetProperty("errors").Deserialize<string[]>();
        Assert.NotNull(errors);
        Assert.NotEmpty(errors);
        Assert.All(errors, error => Assert.False(string.IsNullOrWhiteSpace(error)));
        return errors;
    }
}
