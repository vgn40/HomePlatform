using System.Net;
using System.Text.Json;

namespace HomePlatform.IntegrationTests;

public sealed class OpenApiContractTests
{
    [Fact]
    public async Task Testing_open_api_describes_create_household_and_register_account()
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

        var registration = document.RootElement.GetProperty("paths")
            .GetProperty("/api/accounts/register").GetProperty("post");
        Assert.Equal("RegisterAccount", registration.GetProperty("operationId").GetString());
        Assert.Contains(registration.GetProperty("tags").EnumerateArray(),
            tag => tag.GetString() == "Accounts");
        var registrationResponses = registration.GetProperty("responses");
        Assert.False(registrationResponses.TryGetProperty("200", out _));
        var successSchema = registrationResponses.GetProperty("201")
            .GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString();
        Assert.Equal("#/components/schemas/RegisterAccountResponse", successSchema);
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var accountId = schemas.GetProperty("RegisterAccountResponse")
            .GetProperty("properties").GetProperty("accountId");
        Assert.Equal("string", accountId.GetProperty("type").GetString());
        Assert.Equal("uuid", accountId.GetProperty("format").GetString());

        var errorSchema = registrationResponses.GetProperty("400")
            .GetProperty("content").GetProperty("application/json")
            .GetProperty("schema").GetProperty("$ref").GetString();
        Assert.Equal("#/components/schemas/ProblemDetails", errorSchema);
        var problemProperties = schemas.GetProperty("ProblemDetails").GetProperty("properties");
        foreach (var property in new[] { "type", "title", "status", "detail", "instance" })
        {
            Assert.True(problemProperties.TryGetProperty(property, out _));
        }
        // Dynamic ProblemDetails extensions are covered by the HTTP registration tests.
        // Do not invent a static `errors` schema the framework cannot infer.

    }
}
