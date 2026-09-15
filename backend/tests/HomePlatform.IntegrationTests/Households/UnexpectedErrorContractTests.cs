using System.Net;
using System.Net.Http.Json;
using HomePlatform.Application.Households;
using HomePlatform.Domain.Household;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomePlatform.IntegrationTests.Households;

public sealed class UnexpectedErrorContractTests
{
    private const string InternalDetail =
        "SECRET_POSTGRES_INTERNAL_DETAIL_9F2A";

    [Fact]
    public async Task Unexpected_repository_failure_returns_safe_500()
    {
        var repository = new ThrowingHouseholdRepository();
        await using var factory = new HomePlatformApiFactory(
            "Host=127.0.0.1;Port=1;Database=homeplatform;" +
            "Username=homeplatform;Password=homeplatform-dev",
            services =>
            {
                services.RemoveAll<IHouseholdRepository>();
                services.AddSingleton<IHouseholdRepository>(repository);
            });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/households")
        {
            Content = JsonContent.Create(new { name = "Mit hjem" })
        };
        request.Headers.Add(
            TestAuthenticationHandler.AccountIdHeaderName,
            Guid.NewGuid().ToString());

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(1, repository.AddCallCount);

        Assert.Equal(
            HttpStatusCode.InternalServerError,
            response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var forbiddenDetails = new[]
        {
            InternalDetail,
            "InvalidOperationException",
            "DbUpdateException",
            "PostgresException",
            "StackTrace",
            "ConnectionStrings__Database",
            "Host=127.0.0.1",
            "SELECT ",
            "INSERT ",
            "Npgsql"
        };

        foreach (var forbiddenDetail in forbiddenDetails)
        {
            Assert.DoesNotContain(
                forbiddenDetail,
                body,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class ThrowingHouseholdRepository
        : IHouseholdRepository
    {
        public int AddCallCount { get; private set; }

        public Task<Household?> GetByIdAsync(
            Guid householdId,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("CreateHousehold must not read an existing household.");
        }

        public Task UpdateAsync(
            Household household,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("CreateHousehold must not update an existing household.");
        }

        public Task AddAsync(
            Household household,
            CancellationToken cancellationToken = default)
        {
            AddCallCount++;
            throw new InvalidOperationException(InternalDetail);
        }
    }
}
