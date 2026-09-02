namespace HomePlatform.Application.Households.CreateHousehold;

public enum CreateHouseholdOutcome
{
    Success,
    Invalid,
    Unauthenticated
}

public sealed record CreateHouseholdResult
{
    public CreateHouseholdOutcome Outcome { get; }
    public Guid? HouseholdId { get; }
    public string? Name { get; }
    public string? ErrorMessage { get; }

    private CreateHouseholdResult(
        CreateHouseholdOutcome outcome,
        Guid? householdId = null,
        string? name = null,
        string? errorMessage = null)
    {
        Outcome = outcome;
        HouseholdId = householdId;
        Name = name;
        ErrorMessage = errorMessage;
    }

    public static CreateHouseholdResult Success(
        Guid householdId,
        string name)
    {
        return new CreateHouseholdResult(
            CreateHouseholdOutcome.Success,
            householdId,
            name);
    }

    public static CreateHouseholdResult Invalid(
        string errorMessage)
    {
        return new CreateHouseholdResult(
            CreateHouseholdOutcome.Invalid,
            errorMessage: errorMessage);
    }

    public static CreateHouseholdResult Unauthenticated()
    {
        return new CreateHouseholdResult(
            CreateHouseholdOutcome.Unauthenticated);
    }
}
