# HomePlatform C#/.NET DDD six-month masterplan

> **ARCHIVED — NON-AUTHORITATIVE:** Point-in-time planning baseline from
> 2026-08-27. Use the current
> [six-month roadmap](../../../architecture/roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md)
> and [next steps](../../../architecture/roadmap/NEXT-STEPS.md). Current-state
> diagnoses and UserId-based mapping assumptions below are historical.

Document ID: `HOMEPLATFORM-CSHARP-DDD-6-MONTH-MASTERPLAN-01`  
Audit date: 2026-08-27  
Repository: repository root  
Branch/HEAD inspected: `main` at `a87a8176676a118a2a684f02d0a2ab7f74eef182`  
Planning horizon: approximately 26 weeks  
Implementation status of this document: planning only; no source code changed

## Executive decision

HomePlatform should remain one ASP.NET Core modular monolith with four production projects: Api, Application, Domain, and Infrastructure. Keep those project boundaries, organize code inside them by plural business feature folders, and complete one behavior at a time. Do not split modules into assemblies or services during this plan.

The next six months should produce a small but coherent beta: authenticated people can create a household, invite members, coordinate tasks and recurring routines, manage a shared shopping list and basic household events, and use a Today view. The final weeks turn that product into a reproducibly deployed, observable, and security-reviewed Azure beta.

The current phase is **Phase 1 — Stabilize and complete the CreateHousehold vertical slice**. The immediate stop-gate is not a new feature: restore the namespace/role/test alignment and a clean build before persistence work.

## Scope decisions

### In the six-month target

- ASP.NET Core Identity-based account lifecycle
- trusted authenticated user context
- resource-aware household authorization
- household creation, membership, invitations, and roles
- tasks and deliberately small recurring-routine semantics
- one household shopping list
- basic manually entered household events
- an Application-level Today query
- EF Core migrations and real PostgreSQL integration proof
- Docker, CI/CD, observability, security hardening, and an Azure beta deployment

### Outside the six-month target unless an exit gate finishes early

- children as a separately modeled product area
- pickup/dropoff and school workflows
- grandparents/babysitters beyond the generic Guest role
- meals, expenses, home maintenance, or household discovery
- external calendar synchronization
- push notifications and real-time collaboration
- microservices, brokers, event sourcing, Redis, Kubernetes, or multi-region deployment

## Current-state audit

### Repository safety and verification state

The repository was inspected in place and was already dirty. The audit preserved all existing work. At the final audit snapshot, modified tracked files were `.env.example`, `README.md`, `backend/src/HomePlatform.Api/appsettings.Development.json`, `backend/src/HomePlatform.Domain/Household/HouseholdRole.cs`, `backend/tests/HomePlatform.IntegrationTests/HealthEndpointsTests.cs`, and `docker-compose.yml`. The folders `backend/src/HomePlatform.Application/Households/`, `backend/src/HomePlatform.Infrastructure/Persistence/Configurations/`, and `backend/src/HomePlatform.Infrastructure/Persistence/Repositories/` were untracked. The two configuration files existed but were empty; `HouseholdRepository.cs` already contained an unregistered Add-plus-`SaveChangesAsync` implementation. These changes are user-owned and must be inspected and completed, never recreated or overwritten.

Verification on 2026-08-27:

- **PASS:** .NET SDK 10.0.400 is available.
- **PASS:** Docker daemon is available; server version reported 28.5.1.
- **PASS:** `docker compose config` resolves a PostgreSQL 18.6 service with a health check.
- **PASS:** Application tests ran 1/1 green.
- **PASS:** existing PostgreSQL/Testcontainers integration tests ran 3/3 green.
- **FAIL:** `dotnet build backend/HomePlatform.slnx --no-restore --disable-build-servers -m:1` fails with six `CS0117` errors because tests still use `Parent`/`Child` while the dirty enum now contains `Owner`/`Member`/`Guest`.
- **PASS (point-in-time):** audit-time NuGet scan reported no known vulnerable direct/transitive packages. Advisory data changes; keep connected auditing enabled in CI.
- **FAIL:** `dotnet format --verify-no-changes` reported 30 whitespace/final-newline diagnostics in the current dirty checkout.
- **NOT VERIFIED:** production, email delivery, native client, Azure, backup restore, or load behavior because none exists yet.

The previously green baseline is therefore historical evidence, not the state of the inspected checkout.

### Architecture health score: 5/10

| Area | Score | Evidence |
|---|---:|---|
| Dependency direction | 8/10 | Domain has no references; Application references Domain; Infrastructure references Application and Domain; Api references Application and Infrastructure. Dependency tests exist. |
| Domain model | 5/10 | Useful Household/Member invariants exist, but owner-at-creation, collection encapsulation, length bounds, identity ownership, and role vocabulary remain unresolved. |
| Application | 4/10 | CreateHousehold is explicit and small, but untracked, untested, unregistered, and ambiguous about durable commit semantics. |
| Persistence | 2/10 | Npgsql and DbContext registration exist; two empty mapping placeholders and an unregistered repository implementation are untracked, while the active model, migrations, constraints, DI registration, and round-trip tests do not exist. |
| API | 3/10 | ProblemDetails, development OpenAPI, liveness, and readiness exist; there is no product endpoint or error contract. |
| Security | 1/10 | No authentication, authorization, current-user boundary, rate limiting, HTTPS production configuration, or security integration suite. |
| Testing | 5/10 | Correct project split and real PostgreSQL foundation exist, but the current solution does not compile and the first use case has no tests. |
| Operations | 3/10 | Dockerized PostgreSQL and health endpoints exist; there is no API container, CI/CD, deployment, metrics, runbook, or backup proof. |

The architecture direction is healthier than the delivery maturity. The score should rise only when the first persisted, secured vertical slice is proven—not by adding more abstractions.

### Project-by-project inventory

#### `HomePlatform.Domain`

Already present:

- `Household` with generated ID, trimmed nonblank name, UTC timestamps, a private member list, duplicate-member prevention, and update timestamp behavior.
- `HouseholdMember` with nonempty UserId and enum validation.
- a dirty `HouseholdRole` change from Parent/Child/Guest/Other to Owner/Member/Guest.
- a simple non-generic `Result`.
- `User` with username/email trimming and nonblank validation.

Gaps and risks:

- `HouseholdRole` is currently in the global namespace and conflicts with tests.
- `Members => _members` exposes the actual list behind an interface; a caller can downcast and bypass `AddMember`. Return a read-only wrapper or defensive snapshot.
- a public Household constructor creates an empty household, so “every persisted household has an owner” is not protected by the aggregate.
- no domain maximum length matches a future database limit.
- direct `DateTime.UtcNow` and `Guid.NewGuid()` are acceptable now, but the timestamp test uses `Thread.Sleep(1)` and is nondeterministic.
- current `User` overlaps the fields ASP.NET Core Identity will own. Do not persist it before deciding whether a separate app profile is actually needed.
- singular `Household`/`User` folders and lowercase `common` conflict with the plural/PascalCase direction in architecture notes.

#### `HomePlatform.Application`

Already present in the dirty untracked slice:

- feature folder `Households/CreateHousehold`.
- `CreateHouseholdCommand(CreatorUserId, Name)`.
- concrete `CreateHouseholdHandler` without MediatR.
- `IHouseholdRepository.AddAsync` as a specific outbound port.
- success response `CreateHouseholdResult` rather than a Domain entity.
- async persistence call with CancellationToken propagation.

Gaps and risks:

- no behavior tests, handler registration, authorization boundary, or stable expected-error mapping.
- `CreatorUserId` has no safe place in the target command shape; replace it with injected `ICurrentUser` before adding the Testing-only HTTP route, and never accept it from JSON.
- `AddAsync` does not say whether it only tracks or actually commits. For this one-aggregate slice, define it to persist atomically with one `SaveChangesAsync` call.
- a failed `AddMember` becomes `InvalidOperationException`; this is currently an impossible path for a fresh aggregate, but expected failures must never accidentally become HTTP 500 later.
- the Application README says no use cases exist and is stale.

#### `HomePlatform.Infrastructure`

Already present:

- references only Application and Domain.
- central EF Core/Npgsql packages.
- required connection-string validation.
- scoped `HomePlatformDbContext` registration with `UseNpgsql`.
- empty, sealed DbContext.
- documented migration directory.
- two empty, untracked configuration placeholders and an untracked `HouseholdRepository` that calls Add then one `SaveChangesAsync`; none is active because mappings/DbContext/DI/tests are absent.

Missing:

- DbSets/active model configuration, backing-field relationship mapping, value conversion, database constraints, repository review/registration, migrations, development seeding decision, concurrency policy, and migration tests.

#### `HomePlatform.Api`

Already present:

- Minimal API composition root.
- ProblemDetails and exception handler.
- development-only OpenAPI document.
- `/health` liveness and `/ready` PostgreSQL readiness.
- Api references Application and Infrastructure as intended.

Missing:

- product route groups, boundary DTOs, handler registration, authentication/authorization middleware, current-user mapping, explicit error mapping, rate limiting, CORS policy, HTTPS/reverse-proxy production settings, request IDs, and product OpenAPI response contracts.

#### Tests

Already present:

- Domain, Application, and Integration test projects.
- assembly dependency-direction tests.
- basic Domain tests.
- real PostgreSQL 18.6 Testcontainers health/readiness tests.

Missing or weak:

- Domain tests currently do not compile.
- no Application behavior test exists.
- no EF mapping, migration, repository, HTTP product-flow, resource-authorization, or concurrency test exists.
- dependency tests prohibit named outer HomePlatform assemblies but do not prevent direct EF Core/ASP.NET/Npgsql package leakage into Domain.
- the strict timestamp test sleeps and can flake.
- Testcontainers setup is fine at this size; extract a shared fixture only when a second integration class creates costly duplication.

#### Configuration, Docker, documentation, and conventions

Strengths:

- SDK pinned through `global.json`.
- nullable references, implicit usings, and warnings-as-errors are centrally enabled.
- package versions are centralized.
- `.env` and local appsettings are ignored.
- Compose pins PostgreSQL 18.6 and has a health check and persistent local volume.
- five ADRs correctly record modular monolith, C#/.NET, Domain independence, PostgreSQL, and EF Core decisions.

Gaps:

- root and project READMEs still describe a featureless foundation.
- README suggests copying `.env`, but ASP.NET Core does not load `.env` by itself; Compose does. Clarify the distinction.
- local PostgreSQL publishes on all host interfaces; bind `127.0.0.1` for local-only use.
- `AllowedHosts` is unrestricted and only acceptable before deployment.
- the README installs a floating global `dotnet-ef 10.*`; use a checked-in local tool manifest with a pinned compatible version.
- no `.github/workflows`, Dockerfile for the API, IaC, deployment runbook, or production configuration guide exists.
- dirty new C# files lack final newlines required by `.editorconfig`.
- `frontend/` contains only a README placeholder—no package manifest or client source—so there is no frontend project to build or audit yet. That is consistent with this backend-only planning task.

## Six-month delivery sequence

The durations are budgets, not promises. A phase does not end because its weeks elapsed; it ends only when its exit gate passes. Do not start the next business module while the prior phase has red tests, an unapplied migration, or an unresolved authorization gap.

## Phase 1 — Stabilize and complete CreateHousehold

**Duration:** weeks 1–3

### Goal

Restore a green baseline and turn CreateHousehold into the first complete, PostgreSQL-backed vertical slice through a Testing-only HTTP entry point until real authentication arrives.

### Why now

The current solution is compile-red, role vocabulary is about to become a database contract, and the first use case stops before persistence. Every later feature depends on proving one end-to-end pattern first.

### User value

A developer or integration client can create a named household and receive a stable response. This is not yet a public multi-user feature.

### Architecture value

- freezes naming, feature-folder, transaction, DTO, error, migration, and test conventions.
- proves private Domain state can be persisted without public mutable setters.
- proves the dependency inversion from Application port to Infrastructure repository.
- establishes one command = one aggregate transaction = one SaveChanges call.

### C#/.NET learning goals

- classes, objects, constructors, fields, properties, access modifiers, `readonly`, and `sealed`.
- enums with deliberate stable values.
- records for commands/results and why they are not Domain entities.
- `static` for stateless endpoint-mapping/factory helpers, with an explicit warning that static mutable global state is not application state management.
- collections, `IReadOnlyCollection`, LINQ `Any`, and collection encapsulation.
- interfaces at a real I/O boundary.
- nullable reference types and argument guards.
- `async`/`await`, `Task<T>`, CancellationToken, DI lifetimes, and exception flow.
- EF Core configuration classes, migrations, navigation mapping, and PostgreSQL types.
- Minimal API route groups, typed results, and ProblemDetails.

### Domain concepts

- `Household` as aggregate root.
- `HouseholdMember` as entity inside the aggregate, identified by UserId within a household.
- invariant: a persisted household has a nonblank bounded name and at least one Owner.
- invariant: a user appears no more than once in the member collection.
- role vocabulary: Owner, Member, Guest. Parent/Child describe people, not authorization; defer them.
- keep raw Guid identifiers and the simple Result for now.

### Application use cases

- complete and test `CreateHousehold` only.
- make `CreateHouseholdCommand` contain Name only; the handler injects a minimal Application `ICurrentUser` with a `TryGetUserId`/nullable Guid contract and never accepts an actor ID in a command.
- represent expected invalid-name input as an explicit Application validation outcome/stable error code; never globally translate every `ArgumentException` into HTTP 400.
- represent missing/malformed current user as a distinct unauthenticated outcome; the Api maps it to 401 and persistence is never called.
- make the repository contract explicitly durable: success means household and initial owner committed atomically.
- no generic repository, mediator, UnitOfWork wrapper, or validation framework.

### Infrastructure work

- inspect and complete the existing empty `IEntityTypeConfiguration<Household>` and `<HouseholdMember>` files.
- map the private `_members` field and use field access; do not add a public mutable collection.
- add a private parameterless persistence constructor with nullable-safe initialization to `Household`; keep the public factory/constructor invariant-safe, then prove EF save, `ChangeTracker.Clear()`, and reload.
- map the child through a shadow `HouseholdId` if that keeps the Domain clean.
- add only a Household DbSet; child persistence remains aggregate-internal.
- inspect the existing untracked `HouseholdRepository.AddAsync`, keep Add plus exactly one `SaveChangesAsync`, and test its durable contract.
- register the repository as scoped.
- use `ApplyConfigurationsFromAssembly` in DbContext.
- add a pinned local `dotnet-ef` tool manifest and generate the first migration.

### API work

- add an `/api/households` route group and `POST /api/households`, but map this product route only when the host environment is `Testing` during Phase 1.
- request DTO: `{ "name": "..." }`; never bind CreatorUserId.
- response DTO: household ID and name; do not serialize Household.
- return 201 for success and map only the explicit expected validation result to RFC 7807 ProblemDetails; unexpected exceptions remain safe generic 500s.
- map the distinct current-user-unavailable outcome to 401 with zero mutation, including for an authenticated principal missing a valid Guid subject.
- register a scoped `HttpCurrentUser` using `IHttpContextAccessor`; parse one configured subject/NameIdentifier claim as Guid and fail closed when it is missing or malformed.
- install a real default test authenticate/challenge scheme in the integration host. Do not register/use authentication middleware without a real scheme, and do not map the product route in Development or Production until Phase 2.
- mark `/health` and `/ready` explicitly anonymous when a fallback authorization policy is introduced.
- add explicit OpenAPI response metadata; expose the OpenAPI document only in Development or Testing, never Production, so the Testing-only product contract can be asserted.

### Security work

- restore a trusted identity boundary even before Identity: no client-supplied or command-carried CreatorUserId.
- bound and validate Name consistently in Domain and database.
- do not log request bodies, member IDs as credentials, connection strings, or exception internals.
- keep OpenAPI Development/Testing-only and never map it in Production.
- bind local Compose PostgreSQL to localhost.

### Testing work

- restore Domain test compilation and remove the sleep-based timing assertion.
- add Domain proofs for role validation, duplicate preservation, safe collection exposure, name maximum, and owner-at-creation.
- add hand-written recording-repository Application tests; no mocking library.
- add real PostgreSQL tests that apply migrations, save and reload the aggregate, and prove the private collection round-trips.
- add full HTTP test: request -> handler -> repository -> PostgreSQL -> DTO.
- add a Production-environment host test proving `/api/households` is unmapped/404 in Phase 1, while the Testing host authenticates through the default test scheme.
- test authenticated principals with missing/malformed configured subject claims: return 401 and persist/call nothing.
- request the Testing OpenAPI document and assert the route plus 201/400/401 contracts.
- prove recognized invalid input is 400 and an injected unexpected exception remains a generic 500.
- test blank/oversized names and confirm the repository is not called.
- add baseline CI for restore, build, all tests, and format verification.

### Database work

- `Households`: `Id uuid` primary key, `Name varchar(100)`, `CreatedAt/UpdatedAt timestamptz`, and nonblank/length check where practical.
- `HouseholdMembers`: `HouseholdId uuid`, `UserId uuid`, `Role varchar(...)`, composite primary key `(HouseholdId, UserId)`, FK to Households with cascade, index on UserId, and a role check constraint.
- do not create an authentication user table yet. Phase 1 data is development/test-only; cross-module user integrity is added with Identity.
- no seed data in migrations. Use test fixtures and an optional Development-only seeder later if manual work demands it.

### Files to create

- `backend/tests/HomePlatform.Application.Tests/Households/CreateHousehold/CreateHouseholdHandlerTests.cs`
- Application `ICurrentUser` and Api `HttpCurrentUser` files plus Testing authentication support under integration-test infrastructure.
- `.config/dotnet-tools.json`
- generated initial migration and model snapshot under `backend/src/HomePlatform.Infrastructure/Persistence/Migrations/`
- `backend/src/HomePlatform.Api/Endpoints/Households/CreateHouseholdRequest.cs`
- `backend/src/HomePlatform.Api/Endpoints/Households/CreateHouseholdResponse.cs`
- `backend/src/HomePlatform.Api/Endpoints/Households/HouseholdEndpoints.cs`
- `backend/tests/HomePlatform.IntegrationTests/Households/CreateHouseholdTests.cs`
- `.github/workflows/backend-ci.yml`

### Files to modify

- current Household, HouseholdMember, HouseholdRole, Domain tests, and namespaces/folder casing, including the required private EF persistence constructor.
- current CreateHousehold command, handler, result, and repository contract as tests require.
- the existing empty `HouseholdConfiguration.cs` and `HouseholdMemberConfiguration.cs`, and the existing untracked `HouseholdRepository.cs`.
- `HomePlatformDbContext.cs`, Infrastructure `DependencyInjection.cs`, and Api `Program.cs`.
- stale root/Application/Domain/architecture READMEs after the slice is real.
- local Compose port binding and explanatory environment documentation.

### Files that should not exist yet

- Identity DbContext/user/session tables.
- Task, Routine, Shopping, Calendar, or Today modules.
- `IUnitOfWork`, generic repository, BaseEntity, AggregateRoot base class, mediator, mapper, validator framework, event bus, or domain-event dispatcher.
- API versioning, Redis, queues, workers, WebSockets, or production IaC.

### Dependencies / prerequisites

- settle Owner/Member/Guest semantics and storage before the migration.
- restore a clean build.
- Docker available for Testcontainers.
- explicit agreement that the Phase 1 product endpoint exists only in the Testing environment.

### Risks

- EF cannot bind the planned creator-requiring public constructor; add the private parameterless persistence constructor deliberately and prove backing-field materialization with a round-trip test.
- adding Identity later changes cross-module user integrity; do not preserve Phase 1 development data as production data.
- retaining or using a command/client CreatorUserId would create an immediate identity-confusion/IDOR flaw.
- expanding the first slice into queries, membership, or auth will delay the pattern proof.

### Definition of done

- solution builds with zero warnings/errors.
- all Domain, Application, and Integration tests run green.
- migration applies to an empty PostgreSQL database.
- repository round-trip preserves ID, name, timestamps, Owner, and private members.
- POST persists exactly one household and one Owner atomically and returns a boundary DTO.
- invalid input returns safe ProblemDetails.
- no HTTP request or Application command can select its creator ID; the current-user adapter fails closed.
- the Testing host returns 401 for anonymous requests through a real test scheme, and a Production host proves the product route is 404/unmapped.
- authenticated principals with missing/malformed Guid subject claims receive 401 with zero mutation.
- expected validation is 400 while an unexpected exception remains a generic 500.
- CI repeats the proof.

### Exit gate

**GO only if** the full flow is green against fresh PostgreSQL in the authenticated Testing host and the route is proven absent outside Testing. Otherwise remain in Phase 1.

## Phase 2 — Identity and trusted-user foundation

**Duration:** weeks 4–7

### Goal

Add a secure first-party account lifecycle and replace the temporary creator mechanism with a trusted authenticated user context before any multi-user feature is built.

### Why now

Membership, invitations, and resource authorization cannot be designed safely around arbitrary Guid inputs. Identity is a prerequisite, not polish.

### User value

People can register, confirm email, sign in, refresh a session, sign out, recover a password, and create a household under their real identity.

### Architecture value

- separates authentication from household authorization.
- keeps ASP.NET Core Identity and credential storage in Infrastructure.
- establishes a real Application current-user port.
- resolves whether the existing Domain User is a business profile or redundant credential model.

### C#/.NET learning goals

- generic framework types and Identity stores without writing password cryptography.
- options/configuration, DI lifetimes, claims, authentication schemes, middleware order, and endpoint authorization.
- secure token/cookie concepts, Data Protection, email adapters, rate limiter policies, and integration test authentication.
- pattern matching over authenticated/anonymous state.

### Domain concepts

- credentials are not Domain entities in the household model.
- recommended decision: `ApplicationUser : IdentityUser<Guid>` lives in Infrastructure/Identity.
- keep a Domain `UserProfile` only when a concrete use case needs display name, time zone, or other business behavior; otherwise retire the current username/email Domain User rather than duplicating Identity.
- no Password, PasswordHash, RefreshToken, or Identity framework type in Domain.

### Application use cases

- retain the Phase 1 `ICurrentUser` contract: commands contain target/user-editable data only, while handlers inject the trusted actor port.
- activate the Api `HttpCurrentUser` against the real Identity scheme; it parses the configured subject/NameIdentifier claim as Guid and fails closed.
- keep Identity's account mechanics at the API/Infrastructure boundary instead of wrapping every built-in operation in ceremonial handlers.
- add a small profile-on-registration use case only if a real profile is retained.

### Infrastructure work

- add the Identity EF package and make the existing DbContext inherit roleless `IdentityUserContext<ApplicationUser, Guid>` unless a separate global-role requirement is proven; household roles never justify Identity role tables.
- override `OnModelCreating`, call `base.OnModelCreating(builder)` first, then `ApplyConfigurationsFromAssembly` so Identity and product mappings share one migration chain.
- add `ApplicationUser`, Identity store registration, password/lockout/sign-in options, Data Protection key strategy, and an email sender adapter.
- use a development email sink locally; use a real provider only in staging/beta.
- make the auth ADR select exactly one externally available mode. Default to bearer-only for native: do not expose an unchanged `MapIdentityApi` cookie switch, reject `useCookies=true` (or map a deliberately bearer-only Identity-backed boundary), and remove the Phase 1 fake scheme from the Phase 2 application/test host. If the ADR instead selects cookies, implement the CSRF/CORS cookie branch in this phase.
- configure opaque bearer access/refresh tokens for the first-party native client under that boundary; the client replaces its held refresh token with the newly returned token, but the built-in endpoint does not by itself record token consumption or prove one-time replay detection.
- configure security-stamp invalidation and document the maximum access-token revocation window.

### API work

- group Identity-backed endpoints under `/api/auth`: register, login, refresh, confirm email, resend confirmation, forgot/reset password, and account information. Do not expose `MapIdentityApi` unchanged when its public modes/error shapes violate this contract.
- use a thin Identity-backed login boundary/filter to normalize unknown user, wrong password, unconfirmed/not-allowed, and locked-out failures to the same public 401 ProblemDetails shape while retaining protected internal reason metrics/logs.
- add logout and sign-out-everywhere semantics appropriate to the selected session mode.
- call `UseAuthentication` before `UseAuthorization` and require auth by default for product routes.
- keep health endpoints anonymous.
- return 401 for unauthenticated and the chosen non-enumerating 404/403 policy for unauthorized resources.

### Security work

- use ASP.NET Core Identity's password hasher; never design password crypto.
- require confirmed email before household collaboration.
- configure lockout, password policy, `IdentityOptions.User.RequireUniqueEmail = true`, auth-endpoint rate limits, and non-enumerating responses.
- enforce registration-email uniqueness with a named unique filtered PostgreSQL index/constraint on `NormalizedEmail`; translate only its recognized PostgreSQL `23505` violation into the chosen safe duplicate-registration response, and do not assume Identity's standard index is unique.
- for native Expo, keep opaque tokens in platform secure storage, never AsyncStorage; keep access lifetime short and replace the client-held refresh token with each returned token without claiming one-time rotation, replay detection, or per-device revocation.
- built-in Identity bearer tokens are intentionally proprietary and suitable for a simple first-party client, not a general OAuth/OIDC server. If standard OIDC, third-party delegation, social-login federation, or immediate per-device revocation becomes required, decide on a proven identity server before beta rather than minting custom JWTs.
- prove sign-out-everywhere/security-stamp behavior and document that an existing bearer access token can remain valid only up to its configured lifetime.

### Testing work

- registration, concurrent same-normalized-email registration, duplicate email, weak password, confirmation, login, invalid login, lockout, refresh, reset, logout, and sign-out-everywhere integration tests against a separate real-Identity factory with no Phase 1 fake scheme.
- prove bearer challenges work and `useCookies=true` is rejected under the default ADR; if cookies are selected instead, replace this with explicit cookie/CSRF tests.
- prove unknown user, wrong password, unconfirmed/not-allowed, and locked-out login failures have the same external status/body shape while internal telemetry remains distinguishable.
- document/test whether a previously issued protected refresh token remains reusable until expiry or a security-stamp change; do not write a false “succeeds once” assertion.
- ensure token/password/reset values never appear in logs or ProblemDetails.
- prove anonymous CreateHousehold is 401 and a forged `creatorUserId` request field is rejected (or ignored under a documented unknown-property policy while the trusted claim wins).
- test the Identity migration both from empty and from a Phase 1 database containing HouseholdMember rows.

### Database work

- add standard Identity tables through EF migrations.
- add the named unique filtered `NormalizedEmail` constraint/index and configure `RequireUniqueEmail`; document the exact `23505` constraint-name mapping.
- before adding a HouseholdMembers-to-Identity foreign key, explicitly choose one Phase 1-data strategy: recommended pre-production reset/purge of every dev/test database, a verified backfill of matching Identity users, or defer the FK. Never let arbitrary Phase 1 UserIds make deployment fail accidentally.
- add optional `UserProfiles` only if the corresponding use case exists; key it by Identity UserId and do not duplicate password/security fields.
- add the selected cross-module integrity from HouseholdMembers.UserId to the identity/profile record where the modular-boundary decision permits it.
- keep one PostgreSQL database and one DbContext/migration chain for now.

### Files to create

- an Identity `ApplicationUser` and Identity configuration files in Infrastructure.
- real Identity authentication configuration and updates to the existing Application current-user port/Api adapter.
- email sender adapter and Development email sink.
- auth/security integration tests.
- ADR for Identity/profile ownership and ADR for mobile session mode/revocation semantics.

### Files to modify

- Infrastructure project packages, DbContext, DI, and migrations.
- Api Program/middleware and endpoint mappings.
- CreateHousehold command/handler and tests.
- current Domain User and tests: rename to a justified profile or remove it.
- configuration with secret placeholders, never real production secrets.

### Files that should not exist yet

- home-grown PasswordHasher, JWT issuer, OAuth server, refresh-token crypto, or custom authentication protocol.
- household invitation/membership APIs before the auth exit gate passes.
- external social login unless beta research requires it.
- global Admin role; authorization remains household-scoped.

### Dependencies / prerequisites

- Phase 1 migration and vertical slice green.
- a recorded cookie versus bearer decision for Expo/native and any web client.
- a usable email callback base URL for staging tests.

### Risks

- duplicating username/email across Identity and Domain creates two sources of truth.
- assuming client logout instantly revokes a bearer token creates a false security claim.
- supporting cookies and tokens without a clear client policy increases CSRF/storage mistakes.
- placing household roles in global Identity roles would destroy resource scope.
- existing Phase 1 member UserIds can violate a new Identity FK unless the data-disposition decision is executed and upgrade-tested.

### Definition of done

- account lifecycle works against PostgreSQL with Identity's hasher.
- verified authenticated identity creates households; anonymous and forged identities cannot.
- auth endpoints are rate-limited and lockout is tested.
- session storage and revocation semantics are documented and proven.
- no credential/token appears in source, logs, or error responses.

### Exit gate

**GO only if** trusted user identity reaches Application without client control and all authentication lifecycle tests are green. Do not begin invitations with a temporary auth bypass.

## Phase 3 — Household membership and resource authorization

**Duration:** weeks 8–11

### Goal

Make households safely collaborative through invitations, membership lifecycle, Owner/Member/Guest permissions, and resource-level authorization.

### Why now

Authentication answers who the user is. The core product now needs to answer which household resources that user may see or change before tasks expose household data.

### User value

An Owner can invite people, members can join and see their household, and role-sensitive actions behave predictably.

### Architecture value

- establishes resource authorization as an Application/domain concern rather than an endpoint-only check.
- proves aggregate invariants, database uniqueness, and concurrency defenses agree.
- creates read-query patterns without a CQRS framework.

### C#/.NET learning goals

- richer enum/pattern matching, collection queries, nullable values, records, and result/error codes.
- policy-based and imperative resource authorization.
- unique-constraint handling, optimistic concurrency only where observed, and transactions.
- hand-written fakes for current user/access ports.

### Domain concepts

- Household remains the aggregate for membership mutations while the collection is small.
- invariants: unique member, at least one Owner, last Owner cannot leave/be removed/demoted, roles are valid, accepted invite cannot be accepted twice.
- invitation is a separate entity/aggregate if it has its own lifecycle and expiry; do not force it into Household just for DDD symmetry.
- invitation acceptance is conditional: the opaque token hash is unique, the invitation must still be pending/unrevoked/unexpired, and the authenticated verified user/email must match its target.
- provisional permissions: Owner manages household/membership and coordinates content; Member coordinates content but not roles; Guest has read-only access. Validate this with product use before freezing.

### Application use cases

- `GetMyHouseholds`, `GetHousehold`, and `RenameHousehold`.
- `InviteHouseholdMember`, `AcceptHouseholdInvitation`, `ListHouseholdMembers`.
- `ChangeHouseholdMemberRole`, `RemoveHouseholdMember`, `LeaveHousehold`, and `TransferOwnership` if last-owner flow needs it.
- every handler injects the trusted current-user port and loads/filters by household membership; commands never accept actor IDs.
- queries return DTO projections; commands load an aggregate when enforcing behavior.
- `AcceptHouseholdInvitation` uses one use-case-specific Application/Infrastructure transaction port that loads the invitation and Household in the same scoped DbContext, mutates both, and commits exactly once. Never compose two auto-committing repository writes.
- inside that transaction, bind the verified actor to the invitation target and require an atomic conditional consume/concurrency update to affect exactly one pending row before returning success.

### Infrastructure work

- repository reads by ID plus actor/membership-safe query methods.
- invitation persistence and email link generation.
- a simple sender with explicit resend behavior. Synchronous send after commit is acceptable initially because a saved invitation can be resent; add an outbox only if delivery must survive process failure automatically.
- translate expected unique violations into a stable conflict result.
- add a Household version concurrency token that every membership mutation explicitly changes; map an EF concurrency loser to 409 so simultaneous owner operations cannot both commit from stale state.
- implement the invitation-acceptance transaction port with one scoped DbContext/database transaction covering both aggregate mutations.
- use an invitation concurrency token or conditional `UPDATE ... WHERE pending AND not revoked AND not expired`; exactly one affected row wins, and token lookup uses a unique token-hash index.

### API work

- household get/list/rename endpoints.
- invitation create/accept and member/role endpoints.
- require authentication on the whole product route group.
- do not accept actor IDs; route/body IDs identify the target resource only.
- return 404 rather than revealing inaccessible household existence where appropriate.

### Security work

- define and test a permission matrix.
- protect every resource access from guessed HouseholdId/MemberId/invitation token attacks.
- store only a hash of any opaque invitation secret, enforce token-hash uniqueness, give tokens a single-use expiry, and bind acceptance to the authenticated verified target.
- do not put permissions solely in long-lived claims; membership is mutable resource state.
- log security events without invitation tokens or email bodies.

### Testing work

- Domain tests for last-owner, duplicate, role change, remove/leave, and invitation state transitions.
- Application authorization tests for each role and use case.
- HTTP IDOR matrix: anonymous, nonmember, Guest, Member, Owner, guessed ID, expired/reused invitation.
- PostgreSQL concurrency test proving two simultaneous accepts/adds cannot create duplicate membership.
- inject a failure between invitation and Household mutations and prove the whole acceptance transaction rolls back; barrier-test duplicate requests and two different authenticated acceptors so exactly the bound actor can consume once.
- barrier-test concurrent last-owner demote/remove/leave/transfer attempts and prove at least one Owner remains, with a loser receiving 409.

### Database work

- evolve HouseholdMembers with joined timestamp and any required relationship to Identity/Profile.
- add `HouseholdInvitations` with ID, HouseholdId, normalized target email/user, unique token hash, inviter, role, expiry, accepted/revoked timestamps, and a conditional-consume concurrency mechanism.
- unique partial constraint for active invitation as justified and indexes for household/email/expiry lookups.
- keep role representation compatible with Phase 1.
- add/configure the Household version concurrency token before any membership mutation endpoint is enabled.

### Files to create

- feature folders for each membership/invitation use case in Application and mirrored tests.
- invitation Domain type only when its lifecycle is defined.
- Infrastructure configuration/repository/email adapter files.
- API household membership endpoint DTOs.
- authorization and IDOR integration tests.

### Files to modify

- Household aggregate/member behavior and tests.
- DbContext, migrations, DI, endpoint mapping, OpenAPI, and security documentation.

### Files that should not exist yet

- Tasks, routines, shopping, events, Today projection.
- global role/permission framework, ACL engine, policy DSL, domain service, domain events, or outbox without a delivery requirement.
- social graph or household discovery.

### Dependencies / prerequisites

- Phase 2 trusted Identity and verified email.
- decided role matrix and invitation expiry/resend behavior.
- email sink/provider available in test/staging.

### Risks

- confusing authentication roles with household-scoped roles.
- leaking existence through inconsistent 403/404 behavior.
- keeping all membership checks only in endpoints and bypassing them from future callers.
- race conditions around duplicate membership and last-owner mutation.
- composing the two Phase 1 auto-committing repository writes would split invitation acceptance; the use-case-specific transaction port is mandatory.

### Definition of done

- users see only households they belong to.
- invitation and membership lifecycle works end to end.
- Owner/Member/Guest permissions have one documented matrix and matching tests.
- guessed IDs and concurrent duplicate operations fail safely.
- invitation acceptance is all-or-nothing under injected failure and simultaneous requests.
- a household can never persist without an Owner, including barrier-tested concurrent demote/remove/leave/transfer attempts.

### Exit gate

**GO only if** the complete IDOR/role matrix passes against PostgreSQL, invitation acceptance is proven atomic under injected failure/simultaneous requests, barrier-based owner mutations preserve at least one Owner through the Household concurrency token, and no handler trusts a caller-supplied actor ID.

## Phase 4 — Tasks and recurring household routines

**Duration:** weeks 12–16

### Goal

Deliver the first high-frequency coordination workflow: create, assign, complete, and repeat household tasks without building a general scheduling engine.

### Why now

Identity and household access are stable, so a new module can reuse the proven security, persistence, API, and test patterns. Tasks provide more product value than expanding household administration.

### User value

Household members can see responsibilities, assign work, complete/reopen tasks, and define a small set of recurring routines.

### Architecture value

- proves a second aggregate/module without changing the four-project architecture.
- introduces time modeling and idempotent occurrence creation only when needed.
- establishes read projections and cross-module membership checks without cross-module entity navigation.

### C#/.NET learning goals

- `DateOnly`, `TimeOnly`, `DateTimeOffset`, nullable optional fields, records/value semantics, and pattern matching over state.
- LINQ filtering/projection, async database queries, cancellation, and concurrency exception handling.
- dependency injection of a time boundary only when deterministic recurrence tests demand it.
- SQL indexes/query plans and EF Core `AsNoTracking` projections.

### Domain concepts

- `HouseholdTask` as an aggregate root; avoid naming collisions with `System.Threading.Tasks.Task`.
- task status transitions and completion metadata as invariants.
- `Routine` with the smallest validated recurrence pattern: daily or selected weekdays, optional local time, household time zone.
- use a Value Object for recurrence only because equality/validation/calculation are real behavior.
- Domain Service remains absent unless recurrence calculation cannot naturally live on the pattern/routine.

### Application use cases

- create/list/get/update task.
- assign/unassign to a current household member.
- complete/reopen task with actor and timestamp.
- create/update/pause routine.
- `GetToday`/task queries derive virtual due occurrences without writes.
- an explicit idempotent `CompleteRoutineOccurrence` (or named materialization command) creates the occurrence row only when a user interaction needs durable state; no GET performs inserts.
- authorization uses household access ports; Domain does not query members.

### Infrastructure work

- Task and Routine EF mappings/repositories.
- efficient read projections by HouseholdId/status/due date/assignee.
- idempotent routine occurrence insert with a unique key.
- keep read projections side-effect free; only the explicit command uses the unique `(RoutineId, OccurrenceDate)` insert.
- no hosted worker at first. Introduce an in-process durable worker only if occurrences/notifications must happen without a user request.

### API work

- authenticated household-scoped task and routine endpoints.
- explicit request/response DTOs and pagination/filter defaults.
- idempotent completion semantics where retries are expected.
- no Domain serialization and no free-form recurrence expression.

### Security work

- verify target household and assignee membership server-side.
- Guest remains read-only under the provisional matrix.
- reject cross-household TaskId/AssigneeId combinations and log authorization failures without payload leakage.

### Testing work

- Domain state-transition and recurrence boundary tests.
- Application orchestration/authorization tests.
- PostgreSQL constraints and concurrent occurrence-materialization test.
- an explicit test proves repeated GET/Today reads create no rows.
- HTTP role/IDOR tests and query filtering tests.
- DST, time-zone, missed-day, and pause/resume tests for the exact supported rules.

### Database work

- `HouseholdTasks`: IDs, HouseholdId, title/notes bounds, assignee, status, due local/instant fields, completion metadata, timestamps.
- `Routines`: HouseholdId, recurrence fields, time zone, active state, next/calculation metadata only if needed.
- occurrence link/date with unique `(RoutineId, OccurrenceDate)` to make retries safe.
- indexes driven by list/Today queries; no speculative full-text/search index.

### Files to create

- Domain `Tasks` and `Routines` types plus tests, one behavior at a time.
- Application feature folders and query DTOs.
- Infrastructure mappings/repositories and migration.
- API task/routine endpoints and integration tests.
- ADR for time-zone/recurrence semantics.

### Files to modify

- DbContext, DI, endpoint mappings, authorization matrix, OpenAPI, and Today preparation notes.

### Files that should not exist yet

- RRULE parser, cron designer, Quartz/Hangfire, queue, broker, notification service, push tokens, domain-event bus, or WebSockets.
- arbitrary recurrence exceptions, holiday calendars, school schedules, or assignment optimization.

### Dependencies / prerequisites

- Phase 3 membership/authorization reusable and green.
- product decision on household time zone and supported recurrence rules.
- clear answer for missed occurrence behavior.

### Risks

- recurrence and DST can consume the phase; deliberately cap supported patterns.
- naming a domain entity `Task` creates C# ambiguity.
- background generation without idempotency creates duplicates.
- loading Household plus all Tasks for list queries causes aggregate/query bloat.

### Definition of done

- authorized members can coordinate one-off tasks end to end.
- selected recurring routines generate exactly one occurrence per intended local date.
- unsupported recurrence is rejected rather than silently approximated.
- query performance and indexes are measured on representative beta-sized data.

### Exit gate

**GO only if** time-zone/recurrence behavior is documented and deterministic, occurrence creation is concurrency-safe, and role/IDOR tests remain green.

## Phase 5 — Shopping, basic events, and Today

**Duration:** weeks 17–21

### Goal

Create a coherent daily coordination surface by adding a deliberately small shopping workflow, basic household events, and a Today read projection.

### Why now

The product has secure users, households, and responsibilities. Shopping and events add complementary daily value; Today connects them without forcing one giant aggregate.

### User value

Households can add/check shopping items, record basic events, and see the day's tasks/events plus a shopping summary in one response.

### Architecture value

- demonstrates independent aggregates under one module-oriented monolith.
- introduces a cross-feature read projection without cross-feature write transactions or a CQRS framework.
- separates query DTO composition from Domain behavior.

### C#/.NET learning goals

- more advanced LINQ projection/grouping, query composition, pagination, and immutable DTOs.
- EF Core no-tracking reads, compiled/query diagnostics only if measurement warrants.
- `Task.WhenAll` awareness, including why one scoped DbContext must not execute concurrent operations.
- introduce `IAsyncEnumerable` only if a genuinely large export/streaming use case appears; not for normal lists.

### Domain concepts

- one active `ShoppingList` per household initially, with `ShoppingItem` entities and explicit check/uncheck/remove behavior.
- `HouseholdEvent` with title, all-day or timed start/end, optional location/notes, and basic invariants.
- Today is not an aggregate or Domain Service; it is an Application query over stable projections.
- no external calendar identity, recurrence, attendees, or meal planning.

### Application use cases

- get active shopping list, add/check/uncheck/remove item.
- create/update/delete/list basic household event.
- `GetToday` returns tasks due/overdue, today's events, and shopping count/summary for the current household/local date.
- keep write use cases separate; Today performs reads only.

### Infrastructure work

- Shopping and Events mappings/repositories.
- a dedicated no-tracking Today query implementation returning Application DTOs.
- measure generated SQL and avoid N+1 queries; a few explicit queries are acceptable.
- optimistic concurrency token only if shared-edit tests show last-write-loss is harmful.

### API work

- shopping item and event endpoints under household routes.
- `GET /api/households/{householdId}/today?date=YYYY-MM-DD` with server validation of time zone/date.
- stable mobile-focused DTOs with ISO 8601 instants/local dates and explicit nullable fields.
- no API version prefix until a real incompatible-client requirement exists.

### Security work

- reuse resource authorization on every route and projection query.
- prevent cross-household item/event access even when child IDs are guessed.
- constrain string lengths and payload sizes; avoid logging free-text notes.

### Testing work

- Domain item/event invariants and transitions.
- Application Today composition for empty, mixed, overdue, and time-zone boundary cases.
- PostgreSQL query/integration tests and IDOR matrix for child resources.
- barrier-based concurrent active-list creation test proving the database permits only one active list per household.
- contract snapshots only for important API shapes; avoid brittle whole-OpenAPI snapshots unless intentional.

### Database work

- `ShoppingLists`, `ShoppingItems`, and a unique HouseholdId/partial unique active-list constraint (matching the chosen archive model), plus appropriate status indexes.
- `HouseholdEvents` with `timestamptz` instants and all-day local dates as explicitly modeled fields.
- Today-related indexes on household, due/start date, status.
- do not create a materialized Today table unless profiling proves query cost.

### Files to create

- Domain/Application/Infrastructure/Api feature files for Shopping and Events, mirrored by tests.
- Application Today query/DTO and Infrastructure projection.
- migrations and focused query/authorization integration tests.

### Files to modify

- DbContext, DI, endpoint map, role matrix if product learning changes Guest access, OpenAPI, and frontend contract notes.

### Files that should not exist yet

- external Google/Apple calendar adapters, CalDAV, meal planner, expense ledger, barcode catalog, shopping recommendations, WebSockets, distributed cache, or search service.
- a Today aggregate or cross-module generic query bus.

### Dependencies / prerequisites

- stable Task/Routine queries and time-zone policy.
- validated minimum shopping and event behavior from actual use.
- resource authorization reusable for child resources.

### Risks

- three surfaces can exceed five weeks; prioritize Shopping, then Today over Tasks/Events, then basic Events. Drop Events from the phase before weakening quality gates.
- cross-feature DTOs can couple internal entities if mapping boundaries are skipped.
- collaborative toggles can lose writes; add concurrency only after defining user-visible conflict behavior.

### Definition of done

- the daily coordination loop is usable through documented API DTOs.
- Today remains a read projection and does not mutate modules.
- all child-resource IDOR tests pass.
- the one-active-shopping-list invariant survives concurrent creation.
- representative queries are indexed and free of accidental N+1 behavior.

### Exit gate

**GO only if** Shopping plus Today are coherent and secure. Basic Events are optional at this gate if schedule pressure exists; do not ship three half-finished modules.

## Phase 6 — Beta hardening, CI/CD, observability, and Azure

**Duration:** weeks 22–26

### Goal

Turn the working product into a repeatably deployed and diagnosable public beta with explicit security, migration, backup, and rollback evidence.

### Why now

Operational complexity is justified only after critical product flows exist. It must still enter before public users, not after an incident.

### User value

Beta users get a stable HTTPS service with recoverable data, predictable errors, and monitored critical flows.

### Architecture value

- proves the monolith is deployable as one artifact.
- separates migration identity from runtime least-privilege identity.
- adds measured observability and operational runbooks without a platform rewrite.

### C#/.NET learning goals

- multi-stage Docker builds, environment configuration, Options validation, structured logging, health checks, metrics, tracing, and performance profiling.
- GitHub Actions, artifact promotion, migration bundles, Azure managed identity, Key Vault, and deployment diagnostics.
- delegates/events and deeper runtime/memory concepts only while reading profiles or instrumenting real work.

### Domain concepts

- no new business module by default.
- add audit facts only for a concrete security/support requirement; do not retrofit event sourcing.
- review invariants and module language for clarity with another .NET developer.

### Application use cases

- close beta-critical gaps only: pagination, idempotency, safe retry behavior, account deletion/export if required by beta policy, and operational admin procedures kept outside household authorization.
- no feature expansion while release gates are red.

### Infrastructure work

- production Dockerfile and non-root runtime where supported.
- isolated staging and production Container Apps, PostgreSQL databases, configuration/secrets, managed identities, and Data Protection key scopes; prefer separate Container Apps environments unless an explicitly documented ephemeral-staging design provides equivalent isolation.
- Azure Container Registry, Azure Database for PostgreSQL Flexible Server, Key Vault/managed identity, Azure Blob Storage for the shared Data Protection key ring, and Application Insights/Azure Monitor/Log Analytics.
- persist the shared Data Protection key ring in Blob Storage under a stable application name and protect those keys with a versionless Key Vault key identifier; retain old wrapping-key versions for at least as long as protected Data Protection payloads may need decryption, and grant the Container App identity only required Blob/Key Vault permissions.
- create an immutable image and migration bundle in CI. Use this release sequence: additive/expand schema through a Container Apps Job or controlled private-network runner; backfill plus dual-read/write compatibility proof where needed; start and smoke-test a zero-traffic revision; shift traffic and observe; intentionally close the rollback-to-old-image window; only then run verified contract/drop/rename migrations. Never auto-migrate in API startup.
- runtime database identity receives only data read/write permissions.
- configure email provider, Data Protection key persistence, backup retention, and restore drill.

### API work

- production forwarded-header/HTTPS/HSTS behavior aligned with the hosting proxy.
- exact CORS allowlist; native apps do not need browser CORS, Expo web does.
- request/correlation IDs, ProblemDetails traceId, payload limits, rate-limit policies, and safe production errors.
- OpenAPI contract validation and smoke tests for `/health`, `/ready`, auth, and one household flow.

### Security work

- complete the dedicated Security Roadmap public-beta and production gates.
- secret/key rotation, least privilege, dependency/container scanning, log redaction, secure headers, backup/restore, and incident/runbook review.
- decide whether built-in Identity token revocation semantics meet beta needs; adopt a proven session/identity server only if the recorded requirement exceeds them.
- threat-model auth, invitation, household IDOR, file-free payloads, and operational endpoints.

### Testing work

- CI runs build, unit, PostgreSQL integration, migration-from-empty, format, dependency audit, and container scan.
- staging smoke tests after deploy.
- authentication plus confirmation/reset-token continuity across replica restart and revision replacement, proving the shared Data Protection key ring works.
- rotate the Key Vault wrapping key through its versionless identifier, retain the prior version, and prove old/new protected payloads still work in staging.
- modest load baseline for Today, task list, and login; fix measured bottlenecks only.
- restore a backup to an isolated server and execute a read/write smoke test.
- failure drills: bad migration blocked, unavailable DB yields readiness 503, secret absent fails startup, expired token rejected.

### Database work

- managed PostgreSQL with automated backups and chosen point-in-time retention.
- explicit migration artifact, deployment log, and rollback/forward-fix plan.
- least-privilege runtime role and separate migration role.
- indexes reviewed from measured slow queries; no speculative replicas/partitioning.

### Files to create

- `backend/Dockerfile` and `.dockerignore` if needed.
- deployment and release workflows under `.github/workflows/`.
- simple `infra/` Bicep/Terraform only after the manual architecture is proven; prefer Bicep for Azure learning.
- production configuration/operations, deployment, migration, backup/restore, incident, and release runbooks.
- observability/security tests and beta checklist.

### Files to modify

- Api middleware/configuration, health checks, logging, CORS/rate limits, package audit settings, Compose for API local parity if useful, and all deployment documentation.

### Files that should not exist yet

- Kubernetes manifests, service mesh, API gateway fleet, Kafka/RabbitMQ, Redis cluster, multi-region database, read replicas, microservices, or 24/7 custom operations platform.
- automatic production migration on API startup.

### Dependencies / prerequisites

- product-critical Phase 1–5 exit gates green.
- Azure subscription/budget and selected region.
- production domain/email sender/privacy/support decisions.
- a beta go/no-go owner and rollback authority.

### Risks

- cloud networking and Identity Data Protection configuration can invalidate sessions if treated as defaults.
- migration and API deployment order can break compatibility; prefer expand/migrate/contract changes.
- dropping schema while retaining an old-image rollback path is contradictory; close the observed rollback window explicitly before contract migration.
- sharing staging/production secrets, databases, or Data Protection key scopes can cross-contaminate data or trust; isolation is an explicit gate.
- a green deployment is not backup proof; restore must be rehearsed.
- Container Apps scale-to-zero can create cold starts; measure before setting minimum replicas.

### Definition of done

- one versioned container artifact is promoted through staging to production.
- migrations are explicit, repeatable, and separated from runtime privileges.
- staging/production resources and key scopes are isolated, and tokens survive expected replica/revision replacement through Blob-persisted, Key-Vault-protected keys.
- expand migration precedes traffic shift, the new zero-traffic revision passes smoke tests, and contract cleanup waits until old revisions are retired.
- any required backfill/dual-read-or-write compatibility is proven, the rollback observation window is completed, rollback to incompatible old images is intentionally closed, and only then does contract cleanup run.
- HTTPS, secrets, CORS, rate limiting, logging hygiene, alerts, dependency/container scans, and backup restore pass their gates.
- critical user flows have staging smoke tests and support diagnostics.
- deployment/rollback/restore are documented well enough for another .NET developer.

### Exit gate

**PUBLIC BETA GO only if** deployment, security, migration, and restore evidence is green. Product features alone cannot override a red operational gate.

## First complete vertical slice: exact missing chain

Target flow:

```text
POST /api/households  { name }
        |
        v
Api boundary DTO + trusted current-user source
        |
        v
CreateHouseholdCommand (Name only)
        |
        v
CreateHouseholdHandler + injected ICurrentUser
        |
        v
Household aggregate creates/contains initial Owner
        |
        v
IHouseholdRepository
        |
        v
HouseholdRepository
        |
        v
HomePlatformDbContext + IEntityTypeConfiguration
        |
        v
EF Core migration/model -> PostgreSQL transaction
        |
        v
CreateHouseholdResult -> Api response DTO -> HTTP 201
```

Missing pieces in dependency order:

1. repair Role namespace/vocabulary and tests so the solution builds.
2. settle owner-at-creation, safe Members exposure, name maximum, and plural/PascalCase conventions.
3. add Application handler behavior tests and clarify durable repository semantics.
4. complete the existing empty Household/HouseholdMember mappings, including the private persistence constructor/field materialization contract.
5. apply mappings from DbContext.
6. inspect, register, and integration-test the existing repository with one SaveChanges call.
7. create/apply the initial migration to fresh PostgreSQL.
8. create request/response DTOs and a Minimal API endpoint.
9. supply creator identity through the injected current-user port and a real Testing authentication scheme; never through request JSON/command data.
10. map predictable client input errors to safe ProblemDetails.
11. register/map handler and endpoint in the Api composition root.
12. prove repository round-trip and full HTTP flow with Testcontainers.
13. put the proof in CI and update stale architecture documentation.

## Stable folder and naming convention

Keep four production projects for the entire plan. Use plural feature folders and namespaces, singular type names, PascalCase folders, and test folders that mirror source.

```text
HomePlatform.Domain/
  Common/
  Households/
  Tasks/
  Routines/
  Shopping/
  Events/

HomePlatform.Application/
  Common/Authentication/
  Households/CreateHousehold/
  Households/InviteMember/
  Tasks/CreateTask/
  Today/GetToday/

HomePlatform.Infrastructure/
  Identity/
  Email/
  Persistence/
    Configurations/
    Repositories/
    Queries/
    Migrations/

HomePlatform.Api/
  Endpoints/Households/
  Endpoints/Tasks/
  Endpoints/Shopping/
  Endpoints/Events/
  Endpoints/Today/
  Security/

tests mirror the corresponding source feature paths.
```

Rules:

- one public type per file unless a tiny endpoint-private record is genuinely clearer.
- file-scoped namespaces aligned with folders; final newline; `*Tests` class names.
- `HandleAsync` for asynchronous handlers, or consistently keep `Handle`; choose once in Phase 1.
- no per-module csproj until repeated cross-module reference violations cannot be controlled by folders/tests/review.
- Infrastructure may centralize one DbContext while configurations/repositories remain feature-named.

## Practical testing strategy

### Domain.Tests

Test only pure business behavior:

- constructor/factory invariants, state transitions, entity identity, Value Object equality, recurrence calculation, and collection encapsulation.
- use no database, ASP.NET Core, mocking framework, or DI container.
- do not test trivial getters/setters or chase a coverage percentage.

### Application.Tests

Test orchestration:

- which Domain behavior is invoked, what port receives, cancellation propagation, expected result/error, current-user use, and authorization outcome.
- use small hand-written fakes/recording ports before adding a mocking library.
- do not retest EF mapping or HTTP serialization.

### IntegrationTests

Use real PostgreSQL through Testcontainers and the actual API host:

- apply migrations, never `EnsureCreated`, for schema proof.
- repository save/reload and private-field mappings.
- database constraints, indexes important to behavior, and concurrency races.
- HTTP status, DTO, ProblemDetails, authentication, authorization/IDOR, and end-to-end vertical slices.
- keep the existing per-class container until test time hurts; then introduce a collection fixture and deterministic database reset. Do not share mutable test data.

### CI proof vocabulary

- **PASS** only when the named command/check ran successfully.
- **FAIL** when it ran and failed.
- **NOT VERIFIED** when the environment or artifact does not exist.
- never describe a test project as green when it did not compile or execute.

## DDD learning roadmap

| Concept | Learn/use | Trigger in HomePlatform |
|---|---|---|
| Entity | Phase 1 | Household/HouseholdMember have identity and lifecycle. |
| Invariant | Phase 1 onward | Name, unique member, initial/last Owner, state transitions. |
| Aggregate | Phase 1 | Household plus its member entities form one consistency boundary changed together. |
| Aggregate Root | Phase 1 | Household is the only entry point for membership writes; do not load it for unrelated read projections. |
| Repository | Phase 1 | A real persistence boundary for aggregate storage; specific, not generic. |
| Application service/handler | Phase 1 | Explicit use-case orchestration and I/O sequencing. |
| Value Object | Phase 4 | Recurrence/time rules have validation, equality, and calculation. Do not wrap every string/Guid. |
| Bounded context | Phase 2–3 conceptually | Identity & Access versus Households versus Coordination use different language/ownership; keep them folders/modules, not services. |
| Domain Service | Not by default | Add only when a stateless business rule spans domain concepts and belongs to no entity/value object. First test whether recurrence logic fits RecurrencePattern. |
| Domain Event | Decision gate after Phase 3 | Add only when one committed domain change has multiple independent reactions and direct handler orchestration is coupled. Invitation email alone can be explicit and resendable. |

Domain events do not imply a broker. If reliable post-commit delivery becomes required, first add a simple same-database outbox and in-process worker; add a broker only for independent deployment/scaling or cross-system delivery.

## C# learning progression

| Phase | Language/runtime focus | Feature practice |
|---|---|---|
| 1 | classes, constructors, properties, fields, private/public, readonly, sealed, enums, records, interfaces, collections, LINQ, nullable refs, exceptions, async/await, Task<T>, CancellationToken, and stateless `static` endpoint/factory helpers (never mutable global state) | Household aggregate, handler, repository, endpoint |
| 2 | generics in framework APIs, options, DI lifetimes, claims, pattern matching, secure asynchronous adapters | Identity and current user |
| 3 | richer collection operations, record results/errors, concurrency exceptions | membership and authorization |
| 4 | value semantics, DateOnly/TimeOnly/DateTimeOffset, advanced LINQ where useful, deterministic time | tasks and recurrence |
| 5 | projections/grouping, query performance, optional IAsyncEnumerable only for a real stream | Today/shopping/events |
| 6 | delegates/events while instrumenting, profiling, allocation/memory/concurrency concepts based on measurements | observability and performance |

Do not front-load unsafe code, reflection-heavy metaprogramming, expression-tree frameworks, custom source generators, or premature performance tuning.

## .NET/ASP.NET Core learning progression

| Phase | Framework skills |
|---|---|
| 1 | Minimal APIs, routing, DI, configuration, logging basics, ProblemDetails, OpenAPI, EF Core mappings/migrations, PostgreSQL, Testcontainers, Docker Compose, CI baseline |
| 2 | ASP.NET Core Identity, authentication middleware, bearer/cookie choice, email confirmation/reset, authorization baseline, rate limiting |
| 3 | policies/resource authorization, secure query filtering, unique constraints, concurrency tests |
| 4 | efficient EF queries, time modeling, idempotency, optional BackgroundService decision |
| 5 | no-tracking projections, API contract stability, performance diagnostics |
| 6 | health checks, structured logging, request IDs, metrics/tracing, Docker image, GitHub Actions, Azure, migration bundles, production configuration |

## Employability outcomes

By following the sequence, the developer should be able to demonstrate:

- idiomatic C#, async/cancellation, debugging, and compiler/nullability discipline.
- ASP.NET Core REST APIs, middleware, DI, configuration, ProblemDetails, and OpenAPI.
- EF Core model configuration, migrations, SQL constraints/indexes, transactions, and PostgreSQL behavior, with concepts transferable to SQL Server.
- authentication versus authorization and practical IDOR prevention.
- xUnit unit/application/integration testing with real PostgreSQL.
- Docker, GitHub Actions, Azure basics, logs/health/metrics, deployment, and migration operations.
- Git hygiene and evidence-based architecture decisions.
- pragmatic DDD as a way to protect business behavior, not as a replacement for core .NET skills.

## Database evolution at a glance

| Phase | Add/evolve | Explicitly defer |
|---|---|---|
| 1 | Households, HouseholdMembers, constraints/indexes, initial migration | auth user/profile, future features |
| 2 | roleless Identity tables; unique filtered NormalizedEmail; optional UserProfiles; explicit Phase 1 UserId data/FK decision | sessions beyond proven need |
| 3 | HouseholdInvitations; membership lifecycle fields/indexes; Household concurrency token | global permissions/ACL engine |
| 4 | HouseholdTasks, Routines, occurrence uniqueness | general scheduler/queue |
| 5 | ShoppingLists/Items with one-active-list uniqueness, HouseholdEvents, query indexes | Today materialization/cache |
| 6 | no feature schema by default; least-privilege roles, migration bundle, measured indexes | partitioning, replicas, multi-region |

Migration rules:

- generate a migration per coherent schema change and review it.
- apply all migrations to a fresh PostgreSQL container in CI.
- never call `EnsureCreated` for product integration tests.
- no automatic production migration during API startup.
- use expand/migrate/contract changes once deployed clients/data make destructive changes risky.
- seed only stable reference data. Development/demo users belong in an explicit Development-only seeder, never migrations.

## Frontend/API boundary for React Native/Expo

- Domain objects and EF/Identity entities never cross HTTP.
- Api owns request/response DTOs; Application owns use-case commands/results/query DTOs. Map explicitly.
- request DTOs contain user-editable data only. Actor identity comes from authenticated server context.
- use RFC 7807 ProblemDetails with stable application `code`, validation errors where useful, and `traceId`; do not expose exception details.
- represent IDs as opaque strings/UUIDs, instants as ISO 8601 UTC offsets, and local calendar dates explicitly.
- return only permissions/capabilities useful to render UI; the server still authorizes every action.
- do not add `/v1` until an incompatible client must remain supported. Before public mobile distribution, record a compatibility/deprecation policy because store clients update slowly.
- generate a TypeScript client only after several stable OpenAPI endpoints make generation cheaper than handwritten calls.

## Observability and operations sequence

- Phase 1: keep structured built-in logging, health/readiness, and safe ProblemDetails; add traceId.
- Phase 2–3: log authentication/security events without credentials/tokens; add request/correlation ID propagation.
- Phase 4–5: measure slow queries and key use-case duration; add only metrics that drive an action.
- Phase 6: send structured logs/metrics to Azure Monitor/Application Insights; add traces if external email/background work makes a request hard to follow. One in-process API does not need distributed tracing merely for fashion.
- production diagnostics must answer: which deployment, which request, which user pseudonymous ID, which household/resource type, what status/duration, and whether DB/email failed—without recording secrets or free-text household content.

## Minimal CI/CD roadmap

Phase 1 GitHub Actions on pull requests/pushes:

1. checkout and install SDK from `global.json`.
2. restore with NuGet audit enabled.
3. build with warnings-as-errors.
4. run Domain and Application tests.
5. run Testcontainers integration tests on the Linux runner.
6. apply the entire checked-in migration chain to an empty PostgreSQL database and fail on pending model changes.
7. verify formatting.

Pending-model detection is an explicit check, separate from empty-database migration application:

```bash
dotnet tool restore
dotnet ef migrations has-pending-model-changes \
  --project backend/src/HomePlatform.Infrastructure/HomePlatform.Infrastructure.csproj \
  --startup-project backend/src/HomePlatform.Api/HomePlatform.Api.csproj \
  --context HomePlatformDbContext
```

Phase 2–3 additions:

- auth/IDOR integration suite, upgrade-from-prior-phase migration fixtures, and concurrency gates.

Phase 6 additions:

- build/scan one API container, push immutable tag to ACR.
- create migration bundle as a deployment artifact.
- run additive schema, backfill/compatibility proof, zero-traffic smoke, traffic shift/observation, and explicit rollback-window closure before contract cleanup; use a Container Apps Job/controlled private-network runner and require approval for production promotion.
- retain the previous revision/image only through the documented rollback observation window; afterward prefer forward-fix and do not claim an incompatible old image remains a valid rollback.

## Recommended Azure beta architecture

Use a single Azure region and the smallest managed components that meet the beta gates:

```text
Expo / Web client
      | HTTPS
      v
Isolated Staging/Production Container Apps: HomePlatform.Api
      |                     \
      | Npgsql/TLS           \ logs/metrics
      v                       v
Isolated Azure Database  Application Insights /
for PostgreSQL Flexible  Azure Monitor / Log Analytics
Servers

Container App managed identity -> Blob Data Protection key ring
                                  protected with a Key Vault key
                                  + scoped Key Vault / ACR access
GitHub Actions -> ACR -> staged Container App revision
Container Apps Job/controlled runner -> private PostgreSQL using separate migration identity
```

Container Apps is the default recommendation because it runs one container without managing Kubernetes and supports managed ingress, revisions, secrets, and Azure monitoring. Azure App Service is an acceptable substitution if its pricing/deployment workflow is materially simpler at selection time. Keep Domain and Application unaware of Azure.

Start without PostgreSQL zone-redundant HA if beta budget and downtime tolerance allow, but enable automated backups, choose point-in-time retention, and rehearse restore. Revisit HA from explicit RTO/RPO and usage, not architecture prestige.

## Architectural decision gates

| Do not add yet | Evidence that would justify it |
|---|---|
| Microservices | independent team ownership/deployment or measured scaling isolation outweighs cross-module transactions and operational cost |
| Kafka/RabbitMQ/MassTransit | durable asynchronous work must cross process/service boundaries or scale independently; an in-DB outbox/worker is insufficient |
| Event sourcing | business/legal need to reconstruct all historical states cannot be met by audit records |
| CQRS framework/MediatR | repeated cross-cutting pipeline behavior is clearer and cheaper than direct handler calls; plain command/query separation may remain |
| Redis/distributed cache | measured database latency/load, hot stable reads, and a safe invalidation strategy |
| Background queue/worker | work must happen without a request and survive process failure/retry, such as reliable notifications |
| WebSockets | validated need for sub-second collaboration; polling/refetch is inadequate |
| Multiple DbContexts/schemas | module ownership/migrations conflict or isolation benefit exceeds coordination cost |
| Module-per-project | recurring accidental cross-module compile references cannot be contained through folders/tests/review |
| API versioning | a shipped client cannot migrate in lockstep with an incompatible contract |
| Multi-region | measured user/availability requirement with defined RTO/RPO and budget |
| Domain Service | a real stateless business rule spans concepts and fits no entity/value object |
| Domain Event | one committed business fact has multiple independent reactions and direct orchestration has become coupled |

## End-of-roadmap architectural success

At the Phase 6 exit gate, all statements below require evidence:

- Domain is framework-independent and important invariants live there.
- Application owns use-case orchestration and authorization decisions through ports/domain behavior.
- Infrastructure owns EF Core, PostgreSQL, Identity stores, email, and cloud adapters.
- Api is a thin HTTP/composition boundary with explicit DTOs and safe errors.
- authentication uses a battle-tested stack and authorization is household/resource-aware.
- migrations and PostgreSQL mappings are proven on fresh real databases.
- integration tests cover critical account, household, task, shopping, event, and Today flows.
- Docker development and one-container deployment are reproducible.
- CI validates every change; CD promotes an immutable artifact with explicit migrations.
- logs, health, metrics, alerts, backup, restore, and rollback are usable.
- the architecture remains a modular monolith and another .NET developer can navigate it.

## References used for time-sensitive platform choices

- [ASP.NET Core Identity API endpoints and first-party cookie/token guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0)
- [ASP.NET Core resource-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0)
- [ASP.NET Core rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)
- [ASP.NET Core Data Protection key storage providers](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0)
- [ASP.NET Core Data Protection Key Vault rotation guidance](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0)
- [EF Core backing fields](https://learn.microsoft.com/en-us/ef/core/modeling/backing-field)
- [EF Core entity constructors](https://learn.microsoft.com/en-us/ef/core/modeling/constructors)
- [EF Core production migration strategies](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
- [Azure Container Apps overview](https://learn.microsoft.com/en-us/azure/container-apps/overview)
- [Azure Database for PostgreSQL Flexible Server overview](https://learn.microsoft.com/en-us/azure/postgresql/flexible-server/service-overview)
- [Azure PostgreSQL backup and point-in-time restore](https://learn.microsoft.com/en-us/azure/postgresql/backup-restore/concepts-backup-restore)
