using HomePlatform.Api;
using HomePlatform.Api.Households;
using HomePlatform.Api.Identity;
using HomePlatform.Application;
using HomePlatform.Application.Identity;
using HomePlatform.Infrastructure;
using HomePlatform.Api.Accounts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    ICurrentAccount,
    HttpCurrentAccount>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();
app.MapAccountEndpoints();

if (app.Environment.IsDevelopment()
    || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi();
}

app.MapHealthEndpoints();

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapHouseholdEndpoints();
}

app.Run();

public partial class Program;
