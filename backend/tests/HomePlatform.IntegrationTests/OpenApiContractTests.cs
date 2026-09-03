using System.Net;
using System.Text.Json;

namespace HomePlatform.IntegrationTests;

public sealed class OpenApiContractTests
{
    [Fact]
    public async Task Testing_open_api_describes_create_household()
    {
        await using var factory = new HomePlatformApiFactory(
            "Host=127.0.0.1;Port=1;Database=homeplatform;" +
            "Username=homeplatform;Password=homeplatform-dev");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var content = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(content);

        var operation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/households")
            .GetProperty("post");

        Assert.Equal(
            "CreateHousehold",
            operation.GetProperty("operationId").GetString());

        Assert.Contains(
            operation.GetProperty("tags").EnumerateArray(),
            tag => tag.GetString() == "Households");

        var responses = operation.GetProperty("responses");

        Assert.True(responses.TryGetProperty("201", out var created));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("401", out _));

        var createdSchemaReference = created
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();

        Assert.EndsWith(
            "/CreateHouseholdResponse",
            createdSchemaReference);
    }
}
