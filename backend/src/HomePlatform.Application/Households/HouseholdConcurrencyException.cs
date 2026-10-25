
namespace HomePlatform.Application.Households;

public sealed class HouseholdConcurrencyException : Exception
{
    public HouseholdConcurrencyException(Exception innerException)
        : base(
            "The household was modified by another request.",
            innerException)
    {
    }
}
