using HomePlatform.Domain.Person;

namespace HomePlatform.Domain.Tests;

public sealed class PersonTests
{
    [Fact]
    public void Person_has_stable_distinct_identity_and_optional_name()
    {
        var person = new Domain.Person.Person();
        var id = person.Id;
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal(id, person.Id);
        Assert.NotEqual(id, new Domain.Person.Person().Id);
        Assert.Null(person.DisplayName);
        Assert.Equal("Ada", new Domain.Person.Person("  Ada  ").DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Person_rejects_blank_display_name(string name)
        => Assert.Throws<ArgumentException>(() => new Domain.Person.Person(name));
}
