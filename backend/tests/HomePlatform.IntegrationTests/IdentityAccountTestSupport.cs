using HomePlatform.Domain.Person;
using HomePlatform.Infrastructure.Persistence;
using HomePlatform.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HomePlatform.IntegrationTests;

internal static class IdentityAccountTestSupport
{
    public static async Task<Guid> CreateLoginlessPersonAsync(this HomePlatformApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        var person = new Person();
        context.Set<Person>().Add(person);
        await context.SaveChangesAsync();
        return person.Id;
    }

    public static async Task CreateIdentityAccountAsync(
        this HomePlatformApiFactory factory,
        Guid accountId,
        string? email = null)
    {
        Assert.NotEqual(Guid.Empty, accountId);
        email ??= $"account-{accountId:N}@example.com";

        await using var scope = factory.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<HomePlatformDbContext>();
        var person = new Person();
        // Legacy lifecycle fixtures use matching IDs for concise setup. Real
        // registration/resolution tests explicitly prove differing IDs work.
        context.Entry(person).Property(p => p.Id).CurrentValue = accountId;
        context.Set<Person>().Add(person);
        var user = new ApplicationUser
        {
            Id = accountId,
            PersonId = person.Id,
            UserName = email,
            Email = email
        };

        var result = await manager.CreateAsync(user);
        Assert.True(result.Succeeded, string.Join("; ",
            result.Errors.Select(error => $"{error.Code}: {error.Description}")));
        Assert.Equal(accountId, user.Id);
    }
}
