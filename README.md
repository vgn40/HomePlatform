# HomePlatform

HomePlatform is a C#/.NET backend portfolio project for modelling people and shared households, built with ASP.NET Core, Entity Framework Core and PostgreSQL as a layered monolith. I am building it to develop my backend engineering skills through concrete account workflows, explicit domain rules and a separation between application logic and infrastructure. The focus is currently the backend foundation; a user-facing client and everyday household features are future work.

## Current capabilities

- **Accounts:** registration with email and password, bearer-token sign-in and token refresh through ASP.NET Core Identity. Registration creates a linked Person.
- **Household creation:** the authenticated account's Person becomes the initial Owner.
- **Members without accounts:** an Owner can create a Person and add them as a Member or Guest.
- **Multiple owners:** the domain model supports multiple Owners. An Owner can leave when another Owner remains; the last Owner cannot leave.
- **Ownership transfer:** an Owner can transfer their role to an existing non-owner membership whose Person has an Account. The acting Owner becomes a Member; other Owners retain their roles.
- **Household closure:** any Owner can close a household, deleting the household and its memberships while retaining Accounts and Persons.

Account routes are mapped in all environments. **Household HTTP routes are currently enabled only in the `Testing` environment.** There is no household read API or complete invitation/onboarding flow yet. Multiple-owner rules are implemented and covered by tests, but there is no API workflow for adding another Owner.

## Architecture

The backend is a layered monolith with four projects:

```text
Api → Application → Domain
 │        ▲          ▲
 └→ Infrastructure ─┘
```

Arrows indicate project dependencies.

| Project | Responsibility |
| --- | --- |
| `HomePlatform.Api` | HTTP endpoints, request/response mapping, authentication context and dependency wiring. |
| `HomePlatform.Application` | Use-case handlers, input validation and interfaces for persistence and identity services. |
| `HomePlatform.Domain` | Person, Household and membership rules, including ownership and leaving a household. |
| `HomePlatform.Infrastructure` | EF Core persistence, PostgreSQL mappings and ASP.NET Core Identity adapters. |

Domain has no package or project references and remains independent of ASP.NET Core, EF Core and infrastructure concerns. Application depends on Domain and defines the interfaces implemented by Infrastructure, keeping use-case logic separate from database and authentication details.

## Domain model

| Concept | Meaning |
| --- | --- |
| **Account** | Authentication and credentials, implemented as an ASP.NET Core Identity `ApplicationUser` in Infrastructure. Each Account links to one Person. |
| **Person** | The human identity, independent of login credentials. A Person can exist without an Account. |
| **Household** | A group of people whose membership and ownership rules are managed together. |
| **HouseholdMember** | A Person's participation in a Household, with its own `MembershipId`, a required `PersonId` and an Owner, Member or Guest role. |

This separation lets a household include someone who does not sign in, while keeping their identity distinct from their role in that household. A Person can participate in multiple households, but only once in each household.

## Technology

- C# and .NET 10; SDK baseline `10.0.400` in `global.json`.
- ASP.NET Core Minimal APIs, OpenAPI and ASP.NET Core Identity.
- Entity Framework Core 10 with the Npgsql PostgreSQL provider.
- PostgreSQL `18.6-alpine` in Docker Compose and integration tests.
- xUnit, ASP.NET Core `WebApplicationFactory` and Testcontainers.
- GitHub Actions for build, tests, formatting, dependency auditing and migration checks.

Package versions are centrally managed in `backend/Directory.Packages.props`.

## Testing

Tests are organised under `backend/tests/`:

- **Domain tests** exercise identity validation, membership and ownership rules, including multiple Owners and the last-Owner restriction.
- **Application tests** check handler validation, authorisation decisions and interactions with persistence/identity interfaces using test doubles.
- **Integration tests** use PostgreSQL containers to check persistence, constraints and migrations. HTTP tests use `WebApplicationFactory` to verify authentication, status codes, error responses and OpenAPI contracts.

Dependency tests also check that inner layers do not reference outer projects. Integration tests start their own PostgreSQL containers and require Docker.

## Project status and next steps

HomePlatform is under development. Planned next steps include:

- Household read flows and a complete invitation/onboarding flow for existing users.
- Complete optimistic concurrency handling for household membership and ownership changes.
- An account deletion lifecycle.
- A first client and a practical shared-household feature.

## Running locally

Prerequisites: Git, Docker with Docker Compose, and .NET SDK `10.0.400` or a later .NET 10 feature band allowed by `global.json`.

Run these commands from the repository root:

```bash
docker compose up -d --wait postgres
dotnet tool restore
dotnet restore backend/HomePlatform.slnx
dotnet build backend/HomePlatform.slnx --no-restore

export ConnectionStrings__Database='Host=localhost;Port=5432;Database=homeplatform;Username=homeplatform;Password=homeplatform-dev'

dotnet tool run dotnet-ef database update \
  --project backend/src/HomePlatform.Infrastructure/HomePlatform.Infrastructure.csproj \
  --startup-project backend/src/HomePlatform.Api/HomePlatform.Api.csproj

dotnet run --project backend/src/HomePlatform.Api --launch-profile http
```

The API runs at `http://localhost:5080` in Development. Use `/health` for liveness, `/ready` for database connectivity and `/openapi/v1.json` for the OpenAPI document.

The connection string above matches the local Compose defaults. If you override PostgreSQL settings using `.env.example`, update the exported connection string too; the API does not load Compose's `.env` file automatically.

To try the household routes locally, stop the API and run it in Testing with the same exported connection string:

```bash
ASPNETCORE_ENVIRONMENT=Testing dotnet run \
  --project backend/src/HomePlatform.Api \
  --no-launch-profile --urls http://localhost:5080
```

Household routes require a bearer access token obtained through account sign-in.

Run all tests with Docker running:

```bash
dotnet test backend/HomePlatform.slnx --no-build
```
