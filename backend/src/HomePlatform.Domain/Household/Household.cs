namespace HomePlatform.Domain.Household;

using HomePlatform.Domain.Common;

public class Household
{
    public Guid Id { get; }
    public string Name { get; private set; }

    public DateTime CreatedAt { get;}
    
    public DateTime UpdatedAt { get; private set;}
    private readonly List<HouseholdMember> _members = new();

    public IReadOnlyCollection<HouseholdMember> Members => _members;

    public Household(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Household name cannot be empty.", 
                nameof(name));
        }

        var now = DateTime.UtcNow;

        Id = Guid.NewGuid();
        Name = name.Trim();
        CreatedAt = now;
        UpdatedAt = now;  
       
    }

    public Result AddMember(HouseholdMember member)
{
    ArgumentNullException.ThrowIfNull(member);

    if (_members.Any(m => m.UserId == member.UserId))
    {
        return Result.Failure(
            "User is already a member of the household.");
    }

    _members.Add(member);
    UpdatedAt = DateTime.UtcNow;

    return Result.Success();
}


}