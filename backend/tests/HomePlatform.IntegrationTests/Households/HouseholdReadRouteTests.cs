using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HomePlatform.IntegrationTests.Households;

public sealed class HouseholdReadRouteTests
{
    private const string UnusedDatabase =
        "Host=127.0.0.1;Port=1;Database=unused;Username=unused";

    [Theory]
    [InlineData("/api/households")]
    [InlineData("/api/households/11111111-1111-1111-1111-111111111111")]
    public async Task Anonymous_request_returns_401_without_database_access(string path)
    {
        await using var factory = new HomePlatformApiFactory(UnusedDatabase);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Production", "/api/households")]
    [InlineData("Production", "/api/households/11111111-1111-1111-1111-111111111111")]
    [InlineData("Development", "/api/households")]
    [InlineData("Development", "/api/households/11111111-1111-1111-1111-111111111111")]
    public async Task Read_routes_are_only_exposed_in_testing(string environment, string path)
    {
        await using var factory = new NonTestingApiFactory(environment);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed class NonTestingApiFactory(string environment) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("ConnectionStrings:Database", UnusedDatabase);
        }
    }
}
