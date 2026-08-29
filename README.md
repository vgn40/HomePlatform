# HomePlatform

HomePlatform is an early-stage household coordination platform and a pragmatic
.NET domain-driven design portfolio project. The product direction is shared
household membership, tasks and routines, shopping, events, and a read-only
Today view.

> **Current status:** backend foundation and the first `CreateHousehold` slice
> are in progress. Health/readiness endpoints and initial Household domain code
> exist, but no household workflow is exposed end to end and no frontend has
> been implemented. See the [current next steps](docs/architecture/roadmap/NEXT-STEPS.md)
> for the live implementation gate.

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
HomePlatform.Api -> HomePlatform.Application -> HomePlatform.Domain
        |                    ^
        +-> HomePlatform.Infrastructure -----+
```

Domain remains framework-independent. Application owns explicit use cases and
ports. Infrastructure owns persistence and technical adapters. API is the HTTP
boundary and composition root. Business modules remain inside this deployment
until evidence justifies a more complex topology.

The proposed identity model separates a credential-bearing `Account` from a
stable household `Membership`; it is recorded in
[ADR 0006](docs/architecture/adr/0006-separate-account-and-household-membership-identity.md)
and remains **Proposed**, so the first durable membership schema is blocked
pending review.

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

No migration exists yet. Do not create the first membership migration until
ADR 0006 is reviewed and the Domain/Application model is aligned. The
[current next steps](docs/architecture/roadmap/NEXT-STEPS.md) define the gate
and require a repository-local pinned `dotnet-ef` tool plus fresh PostgreSQL
proof.
