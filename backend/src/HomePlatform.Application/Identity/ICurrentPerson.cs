namespace HomePlatform.Application.Identity;

public interface ICurrentPerson
{
    Task<Guid?> GetPersonIdAsync(
        CancellationToken cancellationToken = default);
}
