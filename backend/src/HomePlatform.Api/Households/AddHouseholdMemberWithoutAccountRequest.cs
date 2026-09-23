using System.Text.Json.Serialization;
using HomePlatform.Domain.Household;

namespace HomePlatform.Api.Households;

public sealed record AddHouseholdMemberWithoutAccountRequest(
    string DisplayName,
    [property: JsonConverter(typeof(JsonStringEnumConverter<HouseholdRole>))]
    HouseholdRole Role);
