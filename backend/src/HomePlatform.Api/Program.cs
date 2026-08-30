using HomePlatform.Api;
using HomePlatform.Api.Households;
using HomePlatform.Api.Identity;
using HomePlatform.Application.Households.CreateHousehold;
using HomePlatform.Application.Identity;
using HomePlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    ICurrentAccount,
    HttpCurrentAccount>();

builder.Services.AddScoped<CreateHouseholdHandler>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
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