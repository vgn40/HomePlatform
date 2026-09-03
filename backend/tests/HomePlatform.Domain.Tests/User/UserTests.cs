using HomePlatform.Domain.User;

namespace HomePlatform.Domain.Tests.User;

public class UserTests
{
    [Fact]
    public void Constructor_throws_when_username_is_empty()
    {
        Assert.Throws<ArgumentException>(
            () => new Domain.User.User("", "test@example.com"));
    }

    [Fact]
    public void Constructor_throws_when_email_is_empty()
    {
        Assert.Throws<ArgumentException>(
            () => new Domain.User.User("Victor", ""));
    }

    [Fact]
    public void Constructor_trims_username()
    {
        var user = new Domain.User.User(
            "  Victor  ",
            "test@example.com");

        Assert.Equal("Victor", user.Username);
    }

    [Fact]
    public void Constructor_trims_email()
    {
        var user = new Domain.User.User(
            "Victor",
            "  test@example.com  ");

        Assert.Equal("test@example.com", user.Email);
    }

    [Fact]
    public void Constructor_generates_id()
    {
        var user = new Domain.User.User(
            "Victor",
            "test@example.com");

        Assert.NotEqual(Guid.Empty, user.Id);
    }
}
