using HomePlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HomePlatform.IntegrationTests;

internal static class IdentityAccountTestSupport
{
    public static async Task CreateIdentityAccountAsync(
        this HomePlatformApiFactory factory,
        Guid accountId,
        string? email = null)
    {
        Assert.NotEqual(Guid.Empty, accountId);
        email ??= $"account-{accountId:N}@example.com";

        await using var scope = factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Id = accountId,
            UserName = email,
            Email = email
        };

        var result = await manager.CreateAsync(user);
        Assert.True(result.Succeeded, string.Join("; ",
            result.Errors.Select(error => $"{error.Code}: {error.Description}")));
        Assert.Equal(accountId, user.Id);
    }
}
