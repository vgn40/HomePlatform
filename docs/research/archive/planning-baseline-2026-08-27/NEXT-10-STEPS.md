# HomePlatform: next 10 development steps

> **ARCHIVED — NON-AUTHORITATIVE:** Point-in-time sequence from 2026-08-27.
> Use the current [next steps](../../../architecture/roadmap/NEXT-STEPS.md).
> The blocker and membership assumptions below have changed.

Audit anchor: 2026-08-27, `main` at `a87a8176676a118a2a684f02d0a2ab7f74eef182`.

## Stop-gate before Step 01

The working tree already contains user-owned modified and untracked files. Preserve/checkpoint that work before following this sequence. Do not stash, reset, clean, or overwrite it casually.

The current build is red: `HouseholdRole` has Owner/Member/Guest while `HouseholdMembersTest` still uses Parent/Child. Therefore the first action is a modification, not a new feature file.

**Next file to open:** `backend/src/HomePlatform.Domain/Household/HouseholdRole.cs`  
**Next new file to create after the green-baseline stop-gate:** `backend/tests/HomePlatform.Application.Tests/Households/CreateHousehold/CreateHouseholdHandlerTests.cs`

The steps below are deliberately sequential. Finish the listed test before opening the next step.

## Step 01 — Freeze role language and restore compilation

### Open first

`backend/src/HomePlatform.Domain/Household/HouseholdRole.cs`

### Modify

1. restore the temporary current namespace `HomePlatform.Domain.Household` so Step 01 compiles with the still-singular folder/types; Step 02 moves all three Household files and changes them together to `HomePlatform.Domain.Households`.
2. keep authorization roles `Owner`, `Member`, and `Guest`; do not use `Parent`/`Child` as permission roles.
3. choose stable explicit enum values if the database will store integers, or record the string-conversion decision before migration. Recommended: persist strings for readability and add a database check constraint.
4. add the final newline required by `.editorconfig`.

Then open:

`backend/tests/HomePlatform.Domain.Tests/Household/HouseholdMembersTest.cs`

Align every Parent/Child assertion with the settled semantics. Step 02 performs the exact folder/file convention moves after behavior is green.

### Why

Role names are about to become Domain, database, authorization, and API contracts. The current mismatch produces six `CS0117` compile errors.

### Depends on

Nothing beyond preserving the dirty worktree.

### Then test

```bash
dotnet restore backend/HomePlatform.slnx
dotnet build backend/HomePlatform.slnx --no-restore
dotnet test backend/tests/HomePlatform.Domain.Tests/HomePlatform.Domain.Tests.csproj --no-build
```

### Complete when

The whole solution compiles and all Domain tests execute. Do not treat Application/Integration passes as a green solution while Domain.Tests is skipped by compilation failure.

## Step 02 — Protect the minimum Household aggregate invariants

### Open first

`backend/src/HomePlatform.Domain/Household/Household.cs`

### Modify in this exact file order

1. `backend/src/HomePlatform.Domain/Household/Household.cs`: make aggregate creation accept the creator ID and create one Owner internally; enforce a 100-character Name maximum; return a members view that cannot expose the underlying list; retain private `_members`; add a private parameterless persistence constructor with nullable-safe initialization because EF cannot bind a creator-requiring public constructor.
2. `backend/src/HomePlatform.Domain/Household/HouseholdMember.cs`: retain invariant-safe scalar construction and no public persistence setters.
3. `backend/tests/HomePlatform.Domain.Tests/Household/HouseholdTests.cs`: prove name, initial Owner, private-constructor-visible behavior, and collection encapsulation.
4. `backend/tests/HomePlatform.Domain.Tests/Household/HouseholdMembersTest.cs`: align member/role behavior and naming after Step 01.
5. **Move** `backend/src/HomePlatform.Domain/Household/` -> `backend/src/HomePlatform.Domain/Households/` and update the three moved namespace declarations plus affected usings.
6. **Move** `backend/tests/HomePlatform.Domain.Tests/Household/` -> `backend/tests/HomePlatform.Domain.Tests/Households/` and update the two moved test namespaces/usings.
7. **Move** `backend/src/HomePlatform.Domain/User/` -> `backend/src/HomePlatform.Domain/Users/` and update its namespace/usings; do not otherwise redesign the type before Phase 2.
8. **Move** `backend/tests/HomePlatform.Domain.Tests/User/` -> `backend/tests/HomePlatform.Domain.Tests/Users/` and update its namespace/usings.
9. **Move (two-stage case-only)** `backend/src/HomePlatform.Domain/common/` -> `backend/src/HomePlatform.Domain/Common.__rename__/` -> `backend/src/HomePlatform.Domain/Common/`, then update the Result namespace/usings.
10. **Move (two-stage case-only)** `backend/tests/HomePlatform.Domain.Tests/common/` -> `backend/tests/HomePlatform.Domain.Tests/Common.__rename__/` -> `backend/tests/HomePlatform.Domain.Tests/Common/`, then update the test namespace/usings.
11. **Move** `backend/tests/HomePlatform.Domain.Tests/Households/HouseholdMembersTest.cs` -> `backend/tests/HomePlatform.Domain.Tests/Households/HouseholdMemberTests.cs` and rename the contained class to `HouseholdMemberTests`.
12. **Modify exact external usings** in `backend/src/HomePlatform.Application/Households/CreateHousehold/CreateHouseholdHandler.cs`, `backend/src/HomePlatform.Application/Households/IHouseholdRepository.cs`, and the existing `backend/src/HomePlatform.Infrastructure/Persistence/Repositories/HouseholdRepository.cs` from `HomePlatform.Domain.Household` to `HomePlatform.Domain.Households` before running the Step 02 test gate.

Keep `Guid.NewGuid()` and `DateTime.UtcNow` for now; do not add clock/ID abstractions in this step.

These moves establish the six-month convention before more files multiply: plural feature folders/namespaces (`Households`, `Users`) and PascalCase `Common`. Preserve file history and perform the stated two-stage casing moves on the current case-insensitive filesystem.

### Why

Domain and database must reject the same invalid states. A database-only length/owner rule creates late failures, and the current collection can be mutated by a downcast.

### Depends on

Step 01 role semantics.

### Then test

- blank and 101-character name fail.
- 100-character name succeeds.
- new household contains exactly one Owner with creator UserId.
- the returned Members view cannot be cast/mutated as the underlying list.
- duplicate add leaves state unchanged.

Avoid `Thread.Sleep`. Delete or rewrite the timestamp test deterministically rather than adding a clock abstraction solely for one test.

### Complete when

The aggregate cannot be created in a persistable ownerless state and collection mutation must pass through Domain behavior.

## Step 03 — Create Application behavior tests before persistence

### Create/modify in this exact file order

1. **Create** `backend/tests/HomePlatform.Application.Tests/Households/CreateHousehold/CreateHouseholdHandlerTests.cs` first; this is the next new file after the compile stop-gate.
2. **Create** `backend/src/HomePlatform.Application/Common/Authentication/ICurrentUser.cs`, exposing a `TryGetUserId`/nullable Guid contract with no HTTP type and failing closed when unavailable.
3. **Modify** `backend/src/HomePlatform.Application/Households/CreateHousehold/CreateHouseholdCommand.cs` so the command contains Name only, never an actor ID.
4. **Modify** `backend/src/HomePlatform.Application/Households/CreateHousehold/CreateHouseholdHandler.cs` to inject `ICurrentUser`, validate expected input into a stable Application outcome/code, construct the aggregate with the trusted UserId, and persist it.
5. **Modify** `backend/src/HomePlatform.Application/Households/CreateHousehold/CreateHouseholdResult.cs` only as the stable success/expected-error contract requires.
6. **Modify** `backend/src/HomePlatform.Application/Households/IHouseholdRepository.cs` to document durable Add semantics.

Use a small hand-written recording repository. Do not add Moq/NSubstitute.

### Required test cases

- handler persists exactly one Household.
- captured aggregate has trimmed name and exactly one Owner matching the injected trusted current user.
- returned ID/name match the captured aggregate.
- exact CancellationToken is forwarded.
- invalid name does not call the repository and returns the defined validation outcome; missing/malformed current user returns a distinct unauthenticated outcome, also with zero repository calls.
- cancellation/persistence exception is not converted into false success.

Clarify the port contract in naming/XML documentation or code structure: `AddAsync` success means the aggregate was durably committed for this one-repository use case. No UnitOfWork wrapper is added.

### Why

The handler is currently untested. Tests freeze orchestration before EF details make failures harder to localize.

### Depends on

Steps 01–02.

### Then test

```bash
dotnet test backend/tests/HomePlatform.Application.Tests/HomePlatform.Application.Tests.csproj
```

### Complete when

All handler behavior is proven without a database or HTTP host.

## Step 04 — Complete the existing Household EF mapping

### Inspect and modify the existing user-owned file

`backend/src/HomePlatform.Infrastructure/Persistence/Configurations/HouseholdConfiguration.cs`

### Configure

- table name `households` using the repository's chosen naming convention consistently.
- `Id` as UUID primary key, generated by Domain rather than database.
- Name required, max 100, with a matching nonblank constraint where practical.
- CreatedAt/UpdatedAt as PostgreSQL `timestamptz`.
- `_members` backing-field navigation with field access.
- cascade delete only because members are aggregate-owned children.

Do not add public mutable setters. Step 02 deliberately adds the smallest private parameterless persistence constructor with nullable-safe initialization. Field-map `_members`, then prove save, `ChangeTracker.Clear()`, and reload in Step 10.

### Why

This is the first proof that Domain encapsulation and EF Core can coexist.

### Depends on

Step 02 final Domain shape.

### Then test

At minimum, compile Infrastructure. The real materialization proof arrives after DbContext/migration fixture exists.

### Complete when

The root scalar fields and private navigation are explicitly configured with no persistence attributes in Domain.

## Step 05 — Complete member mapping and activate the EF model

### Inspect and modify the existing user-owned file

`backend/src/HomePlatform.Infrastructure/Persistence/Configurations/HouseholdMemberConfiguration.cs`

### Configure

- shadow `HouseholdId` if the Domain child does not need a parent-ID property.
- composite primary key or unique constraint on `(HouseholdId, UserId)`.
- required UserId and Role.
- Role string conversion plus allowed-value check constraint.
- index on UserId for “my households”.

### Modify

`backend/src/HomePlatform.Infrastructure/Persistence/HomePlatformDbContext.cs`

- add a `DbSet<Household>` for aggregate roots.
- override `OnModelCreating` and call `ApplyConfigurationsFromAssembly` (Phase 2 will call Identity `base.OnModelCreating` first after the context becomes Identity-capable).
- do not expose a public member DbSet that encourages writes around the aggregate.

### Why

Until DbContext applies both configurations, the EF model remains empty and current readiness tests prove only connectivity.

### Depends on

Step 04.

### Then test

Add a temporary model inspection only if necessary; the durable proof is migration plus save/reload in Step 09.

### Complete when

EF discovers both tables/relationship without changing Domain encapsulation.

## Step 06 — Inspect, register, and test the repository transaction boundary

### Inspect and modify the existing user-owned file

`backend/src/HomePlatform.Infrastructure/Persistence/Repositories/HouseholdRepository.cs`

The untracked file already calls `AddAsync` and one `SaveChangesAsync`. Review rather than recreate it:

- implement the Application `IHouseholdRepository`.
- add the aggregate through the Household DbSet.
- call `SaveChangesAsync(cancellationToken)` exactly once.
- do not catch cancellation.
- translate a specific expected PostgreSQL conflict only when the use case has a stable semantic error; do not blanket-catch `DbUpdateException`.

### Modify

`backend/src/HomePlatform.Infrastructure/DependencyInjection.cs`

Register `IHouseholdRepository` -> `HouseholdRepository` as scoped.

### Why

EF DbContext already supplies unit-of-work behavior. A generic repository or separate UnitOfWork wrapper adds no value to this one-aggregate write.

### Depends on

Steps 03 and 05.

### Then test

Infrastructure compiles; repository integration proof is Step 09.

### Complete when

Handler success cannot occur without a committed database transaction.

## Step 07 — Pin EF tooling and generate the first migration

### Create through the .NET tool command

`.config/dotnet-tools.json`

Pin `dotnet-ef` to the repository's current EF Design line, `10.0.4`. From the repository root, create the manifest only if absent, then restore it on every machine/CI run:

```bash
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 10.0.4
dotnet tool restore
dotnet ef migrations add AddHouseholds \
  --project backend/src/HomePlatform.Infrastructure/HomePlatform.Infrastructure.csproj \
  --startup-project backend/src/HomePlatform.Api/HomePlatform.Api.csproj \
  --context HomePlatformDbContext \
  --output-dir Persistence/Migrations
```

If the manifest already exists, update the pinned tool rather than creating a second manifest. Do not use the README's floating global `10.*` installation.

### Generate

Under `backend/src/HomePlatform.Infrastructure/Persistence/Migrations/`:

- first migration, named for the household schema rather than `Initial` if that improves review clarity.
- `HomePlatformDbContextModelSnapshot.cs`.

### Review generated SQL/model

Confirm only `households` and `household_members` appear; no User/Identity/future tables. Verify PK/FK/cascade, unique key, indexes, lengths, timestamps, and Role storage.

### Why

The migration is the first durable database contract and must be reviewed before an API depends on it.

### Depends on

Steps 04–06 and a connection string usable by design-time tooling.

### Then test

Apply all migrations to a fresh PostgreSQL container/database, then inspect the schema. Do not use `EnsureCreated`. With a disposable connection string placed in `HOMEPLATFORM_MIGRATION_DB`:

```bash
dotnet tool restore
dotnet ef database update \
  --project backend/src/HomePlatform.Infrastructure/HomePlatform.Infrastructure.csproj \
  --startup-project backend/src/HomePlatform.Api/HomePlatform.Api.csproj \
  --context HomePlatformDbContext \
  --connection "$HOMEPLATFORM_MIGRATION_DB"
dotnet test backend/tests/HomePlatform.IntegrationTests/HomePlatform.IntegrationTests.csproj
```

### Complete when

Fresh PostgreSQL reaches the expected schema from migration zero with no manual SQL.

## Step 08 — Create the HTTP boundary without exposing Domain or actor identity

### Create in this exact order

1. `backend/src/HomePlatform.Api/Endpoints/Households/CreateHouseholdRequest.cs`
2. `backend/src/HomePlatform.Api/Endpoints/Households/CreateHouseholdResponse.cs`
3. `backend/src/HomePlatform.Api/Security/HttpCurrentUser.cs`
4. `backend/src/HomePlatform.Api/Endpoints/Households/HouseholdEndpoints.cs`

### Contract

- route: `POST /api/households`.
- request JSON contains only `name`.
- implement `HttpCurrentUser` through `IHttpContextAccessor`, parse exactly the configured subject/NameIdentifier claim as Guid, and fail closed when missing/malformed; Step 09 registers it scoped.
- the handler reads actor identity from `ICurrentUser`; never bind or place CreatorUserId in the request, header, query string, or command.
- return 201 with an explicit response DTO.
- map only the stable expected Application validation outcome to 400 RFC 7807 ProblemDetails with code/traceId. Never globally translate all `ArgumentException`; unexpected failures remain generic 500.
- map the distinct current-user-unavailable outcome to 401 with zero mutation, even when an authenticated principal lacks a valid configured Guid subject claim.
- attach explicit OpenAPI 201/400/401 response metadata.
- map the route only when `app.Environment.IsEnvironment("Testing")` and call `.RequireAuthorization()` there. The Testing host supplies a real default authenticate/challenge scheme. Do not map it in Development or Production before Phase 2.

### Why

Commands/results are internal contracts; Domain entities and caller-controlled identity never cross HTTP.

### Depends on

Step 03 Application contract and Step 06 registered port.

### Then test

Compile Api. The authenticated Testing route is exercised in Step 10; a Production-environment host must not expose it.

### Complete when

No request DTO can select the actor and no response serializes a Domain entity.

## Step 09 — Wire the composition root and reusable PostgreSQL test host

### Modify/create in this exact file order

1. **Modify** `backend/src/HomePlatform.Api/Program.cs`: register the concrete handler and `HttpCurrentUser`; map product endpoints only in Testing; install/use authentication and authorization only when an actual scheme is configured; preserve exception/authentication/authorization/endpoint order; mark `/health` and `/ready` `.AllowAnonymous()` when a fallback policy exists; make the OpenAPI document available to the Testing host for the Step 10 contract assertion without exposing product routes in Development/Production.
2. **Create** `backend/tests/HomePlatform.IntegrationTests/Infrastructure/PostgreSqlFixture.cs`.
3. **Create** `backend/tests/HomePlatform.IntegrationTests/Infrastructure/HomePlatformApiFactory.cs`.
4. **Create** `backend/tests/HomePlatform.IntegrationTests/Infrastructure/TestAuthenticationHandler.cs` (or equivalently named handler) and configure it as the Testing host's default authenticate/challenge scheme.

The fixture must start real PostgreSQL, supply the connection string before host construction, apply migrations, and isolate/reset test data deterministically. Extract only now that more than the health test uses it.

### Why

One reusable host prevents every product test from starting its own container while keeping production composition unchanged.

### Depends on

Steps 07–08.

### Then test

Move/update the existing health tests onto the fixture without changing their behavior. `/health` stays 200 and `/ready` stays 200/503 as appropriate.

### Complete when

The real Testing host resolves endpoint -> handler -> repository and authenticates a known principal, while a Production host leaves the product route unmapped.

## Step 10 — Prove the complete slice and put it in CI

### Create in this exact order

1. `backend/tests/HomePlatform.IntegrationTests/Households/CreateHouseholdTests.cs`.
2. `.github/workflows/backend-ci.yml`.

### Required cases

- valid authenticated request returns 201 and stable DTO.
- exactly one household and one Owner persist atomically.
- saved aggregate reloads with private member collection intact.
- blank/oversized name returns the specifically mapped 400 ProblemDetails and zero rows; an injected unexpected exception remains a generic 500 without internals.
- anonymous request returns 401.
- authenticated principals with a missing or malformed configured subject claim return 401 with zero repository calls/rows.
- Production-environment host returns 404 because the Phase 1 product route is absent.
- an extra `creatorUserId` JSON property cannot select another user; reject unknown properties if that is the chosen API policy, or prove it is ignored and the claim wins.
- cancellation/database failure never returns false success.
- OpenAPI contains the route and 201/400/401 contracts.

### Complete the CI workflow

Run:

1. SDK setup from `global.json`.
2. restore with NuGet audit enabled.
3. build with warnings-as-errors.
4. Domain and Application tests.
5. PostgreSQL/Testcontainers integration tests.
6. restore the local EF tool and apply the checked-in chain to a fresh database.
7. run the explicit pending-model command below and fail if it reports drift.
8. `dotnet format --verify-no-changes`.

```bash
dotnet tool restore
dotnet ef migrations has-pending-model-changes \
  --project backend/src/HomePlatform.Infrastructure/HomePlatform.Infrastructure.csproj \
  --startup-project backend/src/HomePlatform.Api/HomePlatform.Api.csproj \
  --context HomePlatformDbContext
```

Supply the same validated design-time connection/configuration used for migration generation; empty-database application and pending-model detection are separate checks.

### Modify after proof, in this exact order

1. root `README.md`.
2. `backend/src/HomePlatform.Application/README.md`.
3. `backend/src/HomePlatform.Domain/README.md`.
4. `docs/architecture/README.md`.

Replace stale “no product features” statements with the exact proven state.

### Why

The slice is complete only when a repeatable clean environment proves it and documentation stops describing the old foundation.

### Depends on

Steps 01–09.

### Then test

```bash
dotnet restore backend/HomePlatform.slnx
dotnet build backend/HomePlatform.slnx --no-restore
dotnet test backend/HomePlatform.slnx --no-build
dotnet format backend/HomePlatform.slnx --verify-no-changes --no-restore
```

### Complete when

All commands are green in CI, migration starts from an empty PostgreSQL database with no pending model change, the authenticated Testing-only `POST /api/households` proves the full path, and Production proves the route absent until Identity activates it in Phase 2.

## After Step 10

Stop adding Household behavior and begin Phase 2 Identity. The next files should establish `ApplicationUser`/Identity EF configuration and the session-mode ADR. Do not build invitations, Tasks, or a public client against the test authentication scheme.
