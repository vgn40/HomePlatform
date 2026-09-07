# HomePlatform

HomePlatform is an early-stage household coordination platform and a pragmatic
.NET domain-driven design portfolio project. The product direction is shared
household membership, tasks and routines, shopping, events, and a read-only
Today view.

> **Current status (2026-09-07):** PostgreSQL-backed CreateHousehold exists behind
> a Testing-only authenticated route. Roleless ASP.NET Core Identity persistence
> and Account Registration are implemented; registration is committed in `e2fca98` and maps an
> anonymous endpoint in every environment. Real sign-in/session authentication,
> Household resource authorization, deployment, and frontend remain incomplete.
> The [DDD/Clean Architecture audit](docs/architecture/DDD-ARCHITECTURE-AUDIT.md)
> preserves historical findings and records the registration follow-up; [next steps](docs/architecture/roadmap/NEXT-STEPS.md)
> owns the executable order. The verified registration baseline is 72/72 tests
> (Domain 25, Application 11, Integration 36).

## Technology

- C# 14 and .NET 10 LTS
- ASP.NET Core
- Entity Framework Core with Npgsql
- PostgreSQL 18
- xUnit and Testcontainers
- planned React Native, Expo, and TypeScript client

## Architecture

HomePlatform targets a modular monolith with four production projects:

```text
Domain         -> no project/package references
Application    -> Domain
Infrastructure -> Application + Domain
Api            -> Application + Infrastructure
```

Domain remains framework-independent. Application owns explicit use cases and
ports. Infrastructure owns persistence and technical adapters. API is the HTTP
boundary and composition root. Business modules remain inside this deployment
until evidence justifies a more complex topology.

The accepted identity model separates a credential-bearing `Account` from a
stable household `Membership`; it is recorded in
[ADR 0006](docs/architecture/adr/0006-separate-account-and-household-membership-identity.md)
and is implemented for the first Household slice. Verified linking/unlinking,
last-Owner concurrency, Account validation, and Membership resource
authorization remain follow-up work.

## Repository layout

```text
backend/src/    production .NET projects
backend/tests/  unit, dependency, and PostgreSQL integration tests
frontend/       planned client
docs/           architecture, roadmap, product, and research documentation
```

## Documentation

Start with the [documentation index](docs/README.md). The primary documents are:

- [target architecture](docs/architecture/target/TARGET-ARCHITECTURE.md)
- [context map](docs/architecture/target/CONTEXT-MAP.md)
- [domain model](docs/architecture/target/DOMAIN-MODEL.md)
- [six-month roadmap](docs/architecture/roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md)
- [current next steps](docs/architecture/roadmap/NEXT-STEPS.md)
- [security roadmap](docs/architecture/roadmap/SECURITY-ROADMAP.md)
- [technical-debt register](docs/architecture/roadmap/TECHNICAL-DEBT-REGISTER.md)

## Local prerequisites

- .NET SDK 10.0.400 or a newer .NET 10 feature band
- Docker with Docker Compose
- Git

## Start PostgreSQL

```bash
docker compose up -d
```

Local defaults are documented in `.env.example`. Copy it to `.env` only when
you need to override them.

## Start the API

```bash
cd backend
dotnet restore
dotnet run --project src/HomePlatform.Api
```

The default development URL is `http://localhost:5080`.

- Liveness: `GET http://localhost:5080/health`
- Readiness: `GET http://localhost:5080/ready`
- Development OpenAPI: `GET http://localhost:5080/openapi/v1.json`

Override the database connection with the standard .NET configuration key:

```bash
ConnectionStrings__Database='Host=localhost;Port=5432;Database=homeplatform;Username=homeplatform;Password=homeplatform-dev' dotnet run --project src/HomePlatform.Api
```

## Build and test

```bash
cd backend
dotnet restore
dotnet build HomePlatform.slnx --no-restore
dotnet test HomePlatform.slnx --no-build
```

Docker must be running for PostgreSQL/Testcontainers integration tests. A
historical green result is not treated as current evidence; verify the live
working tree before starting the next roadmap step.

## Migrations

`InitialHousehold` and `LimitHouseholdNameLength` define the Household schema;
`AddIdentityPersistence` adds ASP.NET Core Identity persistence. The repository
pins dotnet-ef 10.0.11; Testcontainers applies migrations from zero, and the
2026-09-04 model check reported no pending changes. See the
[current next steps](docs/architecture/roadmap/NEXT-STEPS.md) before extending
the schema.
