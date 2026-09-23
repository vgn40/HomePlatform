using HomePlatform.Application.Identity;
using HomePlatform.Application.Accounts.Refresh;
using HomePlatform.Application.Accounts.Register;
using HomePlatform.Application.Accounts.SignIn;
using HomePlatform.Application.Households;
using HomePlatform.Application.Households.AddHouseholdMemberWithoutAccount;
using HomePlatform.Infrastructure.Identity;
using HomePlatform.Infrastructure.Persistence;
using HomePlatform.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
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

        services
            .AddIdentityCore<ApplicationUser>()
            .AddEntityFrameworkStores<HomePlatformDbContext>()
            .AddSignInManager();

        services
            .AddAuthentication(IdentityConstants.BearerScheme)
            .AddBearerToken(IdentityConstants.BearerScheme);

        services.AddScoped<
            IHouseholdRepository,
            HouseholdRepository>();

        services.AddScoped<
            IAddHouseholdMemberWithoutAccountPersistence,
            AddHouseholdMemberWithoutAccountPersistence>();

        services.AddScoped<
            IAccountRegistration,
            IdentityAccountRegistration>();

        services.AddScoped<
            IAccountAuthentication,
            IdentityAccountAuthentication>();

        services.AddScoped<
            IAccountRefresh,
            IdentityAccountRefresh>();

        services.AddScoped<
            IAccountPersonLookup,
            IdentityAccountPersonLookup>();

        services.AddScoped<
            ICurrentPerson,
            IdentityCurrentPerson>();

        return services;
    }
}