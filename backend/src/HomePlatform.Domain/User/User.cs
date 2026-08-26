namespace HomePlatform.Domain.User;

public class User
{
    public Guid Id { get; }
    public string Username { get; private set; } 
    public string Email { get; private set; }

    public User(string username, string email)
    {

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be empty.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }
      
        Id = Guid.NewGuid();
        Username = username.Trim();
        Email = email.Trim();
    }
}