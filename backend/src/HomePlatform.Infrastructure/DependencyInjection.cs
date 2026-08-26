using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HomePlatform.Infrastructure.Persistence;

namespace HomePlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Database' is required. Configure ConnectionStrings__Database.");
        }

        services.AddDbContext<HomePlatformDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
