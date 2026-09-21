namespace HomePlatform.Domain.Person;

public class Person
{
    public Guid Id { get; }
    public string? DisplayName { get; private set; }

    private Person()
    {
    }

    public Person(string? displayName = null)
    {
        if (displayName is not null)
        {
            var normalizedName = displayName.Trim();

            if (normalizedName.Length == 0)
            {
                throw new ArgumentException(
                    "Display name cannot be empty.",
                    nameof(displayName));
            }

            DisplayName = normalizedName;
        }

        Id = Guid.NewGuid();
    }
}
