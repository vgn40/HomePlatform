# HomePlatform

HomePlatform is a shared household/family coordination app.

> **FOUNDATION ONLY**  
> **NO PRODUCT FEATURES IMPLEMENTED**

## Backend

- C# 14 and .NET 10 LTS
- ASP.NET Core
- PostgreSQL 18
- Entity Framework Core with Npgsql
- xUnit and Testcontainers

## Local prerequisites

- .NET SDK 10.0.400 or a newer .NET 10 feature band
- Docker with Docker Compose
- Git

## Start PostgreSQL

```bash
docker compose up -d
```

The local defaults are documented in `.env.example`. Copy it to `.env` only when you need to override them.

## Start the API

```bash
cd backend
dotnet restore
dotnet run --project src/HomePlatform.Api
```

The default development URL is `http://localhost:5080`.

- Liveness: `GET http://localhost:5080/health`
- Readiness: `GET http://localhost:5080/ready`
- Development OpenAPI document: `GET http://localhost:5080/openapi/v1.json`

Override the database connection with the standard .NET configuration key:

```bash
ConnectionStrings__Database='Host=localhost;Port=5432;Database=vores;Username=vores;Password=vores' dotnet run --project src/HomePlatform.Api
```

## Build

```bash
cd backend
dotnet restore
dotnet build --no-restore
```

## Run tests

```bash
cd backend
dotnet test
```

Docker must be running because the integration tests start a real PostgreSQL container.

## EF Core migrations

No migration exists while the model is empty. After persistence mappings are introduced, run from `backend/`:

```bash
dotnet tool install --global dotnet-ef --version 10.*
dotnet ef migrations add InitialCreate --project src/HomePlatform.Infrastructure --startup-project src/HomePlatform.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/HomePlatform.Infrastructure --startup-project src/HomePlatform.Api
```

See [the architecture notes](docs/architecture/README.md) before introducing the first module.
