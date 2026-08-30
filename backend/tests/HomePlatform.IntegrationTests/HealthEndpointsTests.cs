using System.Net;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;

namespace HomePlatform.IntegrationTests;

public sealed class HealthEndpointsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6-alpine")
        .WithDatabase("homeplatform")
        .WithUsername("homeplatform")
        .WithPassword("homeplatform-dev")
        .Build();

    private HomePlatformApiFactory? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new HomePlatformApiFactory(_postgres.GetConnectionString());
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await Client.GetAsync("/health");
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", body?.Status);
    }

    [Fact]
    public async Task Ready_returns_ready_when_postgresql_is_available()
    {
        var response = await Client.GetAsync("/ready");
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ready", body?.Status);
    }

    [Fact]
    public async Task Ready_returns_service_unavailable_when_postgresql_is_unavailable()
    {
        const string unavailableConnection =
            "Host=127.0.0.1;Port=1;Database=homeplatform;Username=homeplatform;Password=homeplatform-dev;Timeout=1;Command Timeout=1";

        await using var factory = new HomePlatformApiFactory(unavailableConnection);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/ready");
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("unavailable", body?.Status);
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

    private HttpClient Client => _client ?? throw new InvalidOperationException("Test fixture is not initialized.");

    private sealed record HealthResponse(string Status);
}
