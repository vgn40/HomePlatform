using HomePlatform.Api.Accounts.Refresh;
using HomePlatform.Api.Accounts.Register;
using HomePlatform.Api.Accounts.SignIn;

namespace HomePlatform.Api.Accounts;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var accounts = endpoints
            .MapGroup("/api/accounts")
            .WithTags("Accounts");

        accounts.MapRegisterAccountEndpoint();
        accounts.MapSignInAccountEndpoint();
        accounts.MapRefreshAccountEndpoint();

        return endpoints;
    }
}