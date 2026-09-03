using HomePlatform.Application.Households;
using HomePlatform.Infrastructure.Persistence;
using HomePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomePlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("Database");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'Database' is required. " +
                "Configure ConnectionStrings__Database.");
        }

        services.AddDbContext<HomePlatformDbContext>(
            options => options.UseNpgsql(connectionString));

        services.AddScoped<
            IHouseholdRepository,
            HouseholdRepository>();

        return services;
    }
}
