# HomePlatform

HomePlatform helps people coordinate shared life across households, relationships
and changing family structures. It is an early-stage .NET/backend portfolio
project, evolving through small product flows and explicit domain decisions.

The direction covers couples and households without children, families with
children, blended families, separated parents, grandparents and other caregivers.
People should retain their identity as family structures change, including a
child participating in more than one Household. These are product goals, not
implemented capabilities. Start with the [product scope](docs/product/PRODUCT-SCOPE.md).

**TARGET:** Account supplies credentials; Person represents the human being;
HouseholdMembership represents participation in one Household. Relationships
between Persons are independent of co-residence. Person and Relationship are
accepted targets; CareCircle is a future care context. None exists in code yet.

## AS-IS implementation — source reviewed 2026-09-20

Source baseline: committed `main@7162c35`.

- Account Registration, bearer sign-in and Refresh are implemented and mapped
  in all environments. Refresh validates expiry and the Identity security stamp.
- CreateHousehold, TransferOwnership, LeaveHousehold and CloseHousehold have
  Domain/Application/persistence/HTTP implementations. **All Household routes
  are Testing-only**, including with real bearer authentication.
- Account and Membership identities are separate. The nullable AccountId FK to
  Identity enforces reference integrity without automatic Account-delete cascade.
- Transfer/leave/close check current Household membership/Owner authority.
  Nonmembers receive 404; known Member/Guest actors receive 403 for Owner-only
  operations. Protected-request Account validity and optimistic concurrency
  remain unfinished.
- The affected Domain/Application/PostgreSQL integration slice passed 117 tests
  locally on 2026-09-20; see [verification](docs/architecture/roadmap/NEXT-STEPS.md#verified-locally).
  Hosted CI, deployment and operational controls were not verified.

DeleteAccount is **not implemented in this committed snapshot**; separate
uncommitted implementation/test work exists in the reviewed working tree.
Household authorization/resource concealment is committed in `7162c35`.
DeleteAccount remains separate, uncommitted and unchanged.
Frontend, invitations/linking, Tasks, Shopping and Events are not implemented.

Read [next steps](docs/architecture/roadmap/NEXT-STEPS.md) for the sole execution
order and verification boundaries, and [target architecture](docs/architecture/target/TARGET-ARCHITECTURE.md)
for the explicit current/future split.

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

The **AS-IS** identity model separates Account and Membership under
[ADR 0006](docs/architecture/adr/0006-separate-account-and-household-membership-identity.md).
[ADR 0007](docs/architecture/adr/0007-person-as-stable-human-identity.md)
adds Person as the accepted **TARGET**, preserving Membership as participation
and ASP.NET Core Identity in Infrastructure. The exact Account–Person link
and migration are still to be designed. [NEXT-STEPS](docs/architecture/roadmap/NEXT-STEPS.md)
prioritizes that incremental foundation; security/concurrency and release work
remain visible in the [debt register](docs/architecture/roadmap/TECHNICAL-DEBT-REGISTER.md).

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
- [historical six-month plan](docs/architecture/roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md)
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

Four migrations are committed: `20260830093643_InitialHousehold`,
`20260830185551_LimitHouseholdNameLength`,
`20260904100319_AddIdentityPersistence`, and
`20260913183105_AddHouseholdMemberAccountReference`. The last adds the AccountId
lookup index and guarding FK. The repository pins dotnet-ef 10.0.11.
Testcontainers tests and CI define migration checks; no fresh model-drift or
migration execution is claimed by this documentation review. See the
[current next steps](docs/architecture/roadmap/NEXT-STEPS.md) before extending
the schema.
