# HomePlatform

HomePlatform is a personal backend project I'm building with C# and .NET. The idea is to manage shared households, including people who don't necessarily have an account.

I've been using it to work on domain modelling, APIs and database design, particularly the parts that get more complicated once you move beyond basic CRUD.

## Current state

The backend currently supports:

- Account registration, sign-in and token refresh
- Household creation and management
- Members without accounts
- Ownership transfers and multiple Owners
- Leaving and closing households

One of the more interesting challenges has been handling ownership. A household can't be left without an Owner, even when multiple requests try to change its membership at the same time. I've implemented optimistic concurrency with EF Core and PostgreSQL to handle those situations.

The project is still in development. Household endpoints are currently limited to the `Testing` environment, and there's no frontend or invitation flow yet.

## Tech

C# / .NET 10, ASP.NET Core, EF Core, PostgreSQL, ASP.NET Core Identity and xUnit.

The backend is split into Domain, Application, Infrastructure and API projects. Tests cover domain rules, application logic and persistence against PostgreSQL.

## Running locally

Requires .NET 10 and Docker.

```bash
docker compose up -d --wait postgres
dotnet tool restore

export ConnectionStrings__Database='Host=localhost;Port=5432;Database=homeplatform;Username=homeplatform;Password=homeplatform-dev'

dotnet tool run dotnet-ef database update \
  --project backend/src/HomePlatform.Infrastructure \
  --startup-project backend/src/HomePlatform.Api

ASPNETCORE_ENVIRONMENT=Testing dotnet run \
  --project backend/src/HomePlatform.Api \
  --no-launch-profile --urls http://localhost:5080
```

OpenAPI is available at `http://localhost:5080/openapi/v1.json`.

Run the tests with:

```bash
dotnet test backend/HomePlatform.slnx
```

## Next

Household read endpoints, invitations, account deletion and eventually a frontend with some practical household features.
