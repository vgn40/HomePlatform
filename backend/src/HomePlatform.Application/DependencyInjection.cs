using HomePlatform.Application.Accounts.RegisterAccount;
using HomePlatform.Application.Households.CreateHousehold;
using Microsoft.Extensions.DependencyInjection;

namespace HomePlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<RegisterAccountValidator>();
        services.AddScoped<RegisterAccountHandler>();
        services.AddScoped<CreateHouseholdHandler>();

        return services;
    }
}
