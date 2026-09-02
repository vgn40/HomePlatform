using HomePlatform.Domain.Household;

namespace HomePlatform.Domain.Tests.Household;

public class HouseholdTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_throws_when_name_is_invalid(string? name)
    {
        Assert.Throws<ArgumentException>(() =>
            new Domain.Household.Household(name!, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_throws_when_owner_account_id_is_empty()
    {
        Assert.Throws<ArgumentException>(() =>
            new Domain.Household.Household("Mit hjem", Guid.Empty));
    }

    [Fact]
    public void Constructor_trims_name()
    {
        var household = new Domain.Household.Household(
            "  Mit hjem  ",
            Guid.NewGuid());

        Assert.Equal("Mit hjem", household.Name);
    }

    [Fact]
    public void Constructor_accepts_name_at_max_length()
    {
        var name = new string(
            'a',
            Domain.Household.Household.MaxNameLength);

        var household = new Domain.Household.Household(
            name,
            Guid.NewGuid());

        Assert.Equal(name, household.Name);
    }

    [Fact]
    public void Constructor_throws_when_name_exceeds_max_length()
    {
        var name = new string(
            'a',
            Domain.Household.Household.MaxNameLength + 1);

        Assert.Throws<ArgumentException>(() =>
            new Domain.Household.Household(name, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_accepts_padded_name_at_max_length_after_trimming()
    {
        var normalizedName = new string(
            'a',
            Domain.Household.Household.MaxNameLength);
        var name = $"   {normalizedName}   ";

        var household = new Domain.Household.Household(
            name,
            Guid.NewGuid());

        Assert.Equal(
            Domain.Household.Household.MaxNameLength,
            household.Name.Length);
        Assert.Equal(normalizedName, household.Name);
    }

    [Fact]
    public void Constructor_generates_id()
    {
        var household = new Domain.Household.Household(
            "Mit hjem",
            Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, household.Id);
    }

    [Fact]
    public void Constructor_sets_created_and_updated_at_consistently()
    {
        var household = new Domain.Household.Household(
            "Mit hjem",
            Guid.NewGuid());

        Assert.NotEqual(default, household.CreatedAt);
        Assert.Equal(household.CreatedAt, household.UpdatedAt);
    }

    [Fact]
    public void New_household_contains_exactly_one_owner_membership()
    {
        var ownerAccountId = Guid.NewGuid();

        var household = new Domain.Household.Household(
            "Mit hjem",
            ownerAccountId);

        var owner = Assert.Single(household.Members);
        Assert.NotEqual(Guid.Empty, owner.MembershipId);
        Assert.Equal(ownerAccountId, owner.AccountId);
        Assert.Equal(HouseholdRole.Owner, owner.Role);
    }

    [Fact]
    public void Members_is_exposed_as_a_read_only_collection()
    {
        var membersProperty = typeof(Domain.Household.Household)
            .GetProperty(nameof(Domain.Household.Household.Members));

        Assert.NotNull(membersProperty);
        Assert.Equal(
            typeof(IReadOnlyCollection<HouseholdMember>),
            membersProperty.PropertyType);
    }
}
