namespace HomePlatform.Application.Identity;

public interface IAccountPersonLookup
{
    Task<Guid?> GetPersonIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAccountForPersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default);
}
