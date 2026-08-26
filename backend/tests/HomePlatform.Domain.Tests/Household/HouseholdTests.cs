using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public class HouseholdTests
{
    [Fact]
    public void Constructor_throws_when_name_is_empty()
    {
        Assert.Throws<ArgumentException>(() => new Domain.Household.Household(""));
    }

    [Fact]
    public void Constructor_throws_when_name_is_whitespace()
    {
        Assert.Throws<ArgumentException>(() => new Domain.Household.Household("   "));
    }

    [Fact]
    public void Constructor_trims_name()
    {
        var household = new Domain.Household.Household("  Mit hjem  ");

        Assert.Equal("Mit hjem", household.Name);
    }

    [Fact]
    public void Constructor_generates_id()
    {
        var household = new Domain.Household.Household("Mit hjem");

        Assert.NotEqual(Guid.Empty, household.Id);
    }

    [Fact]
    public void Constructor_sets_created_and_updated_at()
    {
        var household = new Domain.Household.Household("Mit hjem");

        Assert.NotEqual(default, household.CreatedAt);
        Assert.Equal(household.CreatedAt, household.UpdatedAt);
    }

    [Fact]
    public void New_household_has_no_members()
    {
        var household = new Domain.Household.Household("Mit hjem");

        Assert.Empty(household.Members);
    }
}