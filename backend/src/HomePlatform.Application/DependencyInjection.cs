using HomePlatform.Application.Accounts.Refresh;
using HomePlatform.Application.Accounts.Register;
using HomePlatform.Application.Accounts.SignIn;
using HomePlatform.Application.Households.CreateHousehold;
using HomePlatform.Application.Households.TransferOwnership;
using Microsoft.Extensions.DependencyInjection;

namespace HomePlatform.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<RegisterAccountValidator>();
        services.AddScoped<RegisterAccountHandler>();

        services.AddScoped<SignInAccountValidator>();
        services.AddScoped<SignInAccountHandler>();

        services.AddScoped<RefreshAccountValidator>();
        services.AddScoped<RefreshAccountHandler>();

        services.AddScoped<CreateHouseholdHandler>();
        services.AddScoped<TransferOwnershipHandler>();

        return services;
    }
}