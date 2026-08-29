# HomePlatform target architecture

> **ARCHIVED — NON-AUTHORITATIVE:** Detailed target baseline from 2026-08-27.
> Use the current
> [target architecture](../../../architecture/target/TARGET-ARCHITECTURE.md).
> Current-state and UserId-based membership statements below are historical.

Status: target for the end of the six-month masterplan  
Style: pragmatic modular monolith  
Deployables: one ASP.NET Core application container and one PostgreSQL database per isolated environment  
Source of truth for current state: repository inspected 2026-08-27

## Architecture decision

Keep the current four production projects. They already enforce the correct outer-to-inner dependency direction and are sufficient for the expected beta scope. “Modular” means explicit business feature ownership and controlled dependencies inside the monolith; it does not require one assembly, database, or deployment per feature.

```text
Compile-time project dependencies

HomePlatform.Api
    |---> HomePlatform.Application
    `---> HomePlatform.Infrastructure

HomePlatform.Infrastructure
    |---> HomePlatform.Application
    `---> HomePlatform.Domain

HomePlatform.Application
    `---> HomePlatform.Domain

HomePlatform.Domain
    `---> no HomePlatform or technical project
```

Allowed technical ownership:

- Domain: business state/behavior only.
- Application: use cases, orchestration, authorization decisions, ports, and boundary-neutral DTOs/results.
- Infrastructure: EF Core, Npgsql, Identity persistence, email, clock/technical adapters, migrations, and query implementations.
- Api: HTTP request/response DTOs, routing, authentication principal mapping, middleware, composition, and HTTP error mapping.

The Api may know Infrastructure because it is the composition root. Infrastructure may implement Application ports. Domain must never know ASP.NET Core, EF Core, PostgreSQL/Npgsql, Infrastructure, Api, Identity, logging, email SDKs, or Azure.

## Current architecture versus six-month target

```text
CURRENT (2026-08-27)

health/readiness HTTP only
          |
          v
HomePlatform.Api -----> empty HomePlatformDbContext -----> PostgreSQL connectivity
          |
          +-----> unregistered/unexposed CreateHousehold handler

Domain Household/User exist
Application CreateHousehold files are untracked
Two untracked mapping files exist but are empty
An untracked Add+Save HouseholdRepository exists but is unregistered/unmapped/untested
No active mappings, migrations, product API, auth, CI, or deployment
Current solution build: FAIL because role enum/tests disagree
```

```text
TARGET (approximately 26 weeks)

React Native / Expo / optional web client
                  |
                  | HTTPS + authenticated requests
                  v
        +---------------------------+
        | HomePlatform.Api          |
        | Minimal API route groups  |
        | auth principal -> actor   |
        | DTOs / ProblemDetails     |
        | rate limits / CORS        |
        | health / request IDs      |
        +-------------+-------------+
                      |
                      v
        +---------------------------+
        | HomePlatform.Application  |
        | explicit commands/queries |
        | orchestration/authz       |
        | ports + result DTOs       |
        +-------------+-------------+
                      |
                      v
        +---------------------------+
        | HomePlatform.Domain       |
        | Households/Membership     |
        | Tasks/Routines            |
        | Shopping/Events           |
        | entities/invariants/VOs   |
        +---------------------------+
                      ^
                      |
        +-------------+-------------+
        | HomePlatform.Infrastructure|
        | EF/Npgsql repositories     |
        | query projections          |
        | ASP.NET Core Identity EF   |
        | email/technical adapters   |
        +-------------+--------------+
                      |
                      v
        +---------------------------+
        | PostgreSQL                |
        | one DB, migrations,       |
        | constraints and indexes   |
        +---------------------------+

One API container -> Azure Container Apps
One managed DB -> Azure Database for PostgreSQL Flexible Server
Managed identity -> Key Vault / ACR
Shared Data Protection key ring -> Blob Storage, protected by Key Vault key
Logs/metrics -> Azure Monitor/Application Insights
GitHub Actions -> build/test/scan/migrate/promote
```

## Module map and ownership

Modules are business-language boundaries inside the four projects. They are not services.

| Module | Owns | Does not own |
|---|---|---|
| Identity & Access | credentials, hashing, sign-in lifecycle, confirmation/reset, authentication session mechanics | household roles or access to a specific HouseholdId |
| Households | household name, membership, invitations, Owner/Member/Guest invariants | passwords, task lifecycle, shopping items |
| Tasks & Routines | task state/assignment/due behavior, supported recurrence | household membership source of truth, calendar integrations |
| Shopping | active list and item behavior | product catalog/recommendations |
| Events | basic household event behavior | external calendar synchronization |
| Today | read-only composition of tasks/events/shopping summary | cross-module writes or a Today aggregate/table by default |

Cross-module rules:

- reference other modules by opaque IDs and narrow Application ports/read contracts, not EF navigation graphs.
- a mutation loads the aggregate that owns the invariant.
- a query can project directly to a read DTO with `AsNoTracking`; it need not hydrate an aggregate.
- GET/read projections never materialize rows. An explicit idempotent command creates a routine occurrence only when interaction requires durable state.
- do not create a shared “Common” dumping ground. Common contains only genuinely shared primitives with more than one proven consumer.
- Identity framework entities stay out of Domain. Household membership can store the authenticated user's Guid without inheriting Identity types.

## Six-month folder convention

Use plural feature folders/namespaces and singular type names. Tests mirror source.

```text
backend/src/HomePlatform.Domain/
  Common/
    Result.cs
  Households/
    Household.cs
    HouseholdMember.cs
    HouseholdRole.cs
  Tasks/
    HouseholdTask.cs
  Routines/
    Routine.cs
    RecurrencePattern.cs
  Shopping/
  Events/

backend/src/HomePlatform.Application/
  Households/
    IHouseholdRepository.cs
    CreateHousehold/
      CreateHouseholdCommand.cs
      CreateHouseholdHandler.cs
      CreateHouseholdResult.cs
  Tasks/
  Routines/
  Shopping/
  Events/
  Today/

backend/src/HomePlatform.Infrastructure/
  Identity/
  Email/
  Persistence/
    HomePlatformDbContext.cs
    Configurations/
    Repositories/
    Queries/
    Migrations/

backend/src/HomePlatform.Api/
  Endpoints/
    Auth/
    Households/
    Tasks/
    Routines/
    Shopping/
    Events/
    Today/
  Security/

backend/tests/ mirrors the corresponding feature paths.
```

Conventions:

- file-scoped namespace equals folder path.
- one behavior/use case per Application subfolder.
- concrete handler, no handler interface and no mediator by default.
- `Async` suffix for I/O methods; choose `Handle` versus `HandleAsync` once and remain consistent.
- sealed commands/results/handlers where inheritance has no purpose.
- private collections and mutation methods in Domain; no lazy-loading proxies.
- one public type per file unless a tiny private/endpoint-local type improves locality.
- generated migrations are the exception to hand-formatted source conventions.

## Write path

Example CreateHousehold:

```text
HTTP request DTO { name }
       |
       | authentication builds ClaimsPrincipal
       v
CreateHouseholdCommand(name)
       |
       v
CreateHouseholdHandler + Application ICurrentUser
       |
       | constructs/calls Domain behavior
       v
Household aggregate with initial Owner
       |
       v
IHouseholdRepository.AddAsync
       |
       v
HouseholdRepository + DbContext
       |
       | one SaveChangesAsync
       v
PostgreSQL transaction
       |
       v
Application result -> Api response DTO -> HTTP 201
```

Rules:

- the request DTO/command never contains actor identity as proof. From Phase 1 onward the handler injects Application `ICurrentUser`; Api's scoped adapter parses one configured subject/NameIdentifier claim as Guid and fails closed.
- the handler is the orchestration boundary and propagates CancellationToken.
- the Domain enforces persistable business invariants.
- the repository is aggregate-specific and owns the simple durable commit for a single-repository command.
- EF's DbContext is the Unit of Work; no wrapper is added.
- `AcceptHouseholdInvitation` is the concrete multi-aggregate case: one use-case-specific Application/Infrastructure port loads invitation and Household in the same scoped DbContext, mutates both, and commits once. Never compose two auto-committing repository writes. Add a broader transaction abstraction only if another proven use case needs it.

## Read path

Example Today:

```text
GET /api/households/{id}/today
       |
       v
GetToday query + trusted actor
       |
       v
resource authorization / membership filter
       |
       v
Infrastructure no-tracking projections
  | tasks | events | shopping summary |
       |
       v
Application Today DTO -> Api response DTO
```

Rules:

- do not load Household plus all child aggregates merely to render a screen.
- query implementations may use EF Core directly inside Infrastructure and return Application-owned DTOs.
- Today is composition, not a Domain aggregate or cross-module transaction.
- `GetToday` derives virtual due routine occurrences without writes; an explicit idempotent completion/materialization command owns the unique occurrence insert.
- no CQRS framework is required to distinguish writes from reads.

## Domain modeling rules

For every new behavior:

1. write the user/use-case statement.
2. identify the business invariant and aggregate that owns it.
3. add only the minimum entity/value object/state needed.
4. write Domain tests for that behavior.
5. add the Application command/query and tests.
6. add a port only for a real external/technical boundary.
7. implement persistence/mapping and migration.
8. expose an Api DTO/endpoint.
9. prove the full flow with PostgreSQL integration tests.
10. stop and review before the next behavior.

Do not pre-create empty feature projects, base types, repositories, or events.

### Aggregate boundaries at the target

- Household protects membership/role mutations while beta household sizes remain small.
- HouseholdTask protects its own lifecycle. It stores HouseholdId/AssigneeUserId but does not contain Household.
- Routine protects recurrence definition; occurrence uniqueness is enforced in both behavior and database.
- ShoppingList protects its item changes; keep one active list per household initially and enforce that rule with a unique HouseholdId/partial active-list database constraint.
- HouseholdEvent protects its own title/time invariants.
- Invitation may be its own aggregate because it has token, expiry, acceptance, revocation, and resend lifecycle.

Aggregate boundaries are transaction boundaries, not universal query boundaries.

## Persistence architecture

### EF configuration

- `IEntityTypeConfiguration<T>` lives in Infrastructure/Persistence/Configurations.
- after Identity is added, use roleless `IdentityUserContext<ApplicationUser, Guid>` unless a real global-role requirement exists; household roles do not justify Identity role tables.
- `HomePlatformDbContext.OnModelCreating` calls Identity `base.OnModelCreating(builder)` first, then applies configurations from the Infrastructure assembly.
- use private backing fields/field access for Domain collections.
- because invariant-safe Household creation requires creator identity EF cannot bind, add a private parameterless persistence constructor with nullable-safe initialization; never expose mutable state just for EF. Prove save, `ChangeTracker.Clear()`, and reload.
- configure every important Domain/database agreement explicitly: max length, required, conversion, relationship, cascade, unique constraint, check constraint, and index.

### Migrations

- one checked-in migration chain and model snapshot.
- a repository-local pinned `dotnet-ef` tool manifest.
- fresh PostgreSQL migration proof in CI.
- explicit migration bundle/script in deployment; API startup does not mutate production schema.
- separate schema-change identity from least-privilege runtime identity.
- review generated destructive operations. After beta data exists, prefer expand/migrate/contract.

### Transactions

- one handler and one aggregate repository call normally produce one EF SaveChanges transaction.
- repository methods must make durable-versus-tracking semantics unambiguous.
- external email is not part of the database transaction. Persist invitation first and support resend.
- invitation acceptance is different from invitation email: the use-case-specific transaction port binds the verified actor to the invitation target, atomically consumes exactly one pending/unrevoked/unexpired invitation through a concurrency token or conditional update, and commits it with Household-member-added. A unique token-hash index, injected failure, duplicate requests, and different-actor attempts prove one winner or full rollback.
- add an outbox only when reliable post-commit automatic delivery is an actual requirement.

### Concurrency

- database uniqueness is the final guard for duplicate membership and routine occurrences.
- before Phase 3 owner mutations, add a Household version concurrency token changed by every membership mutation; map the stale writer to 409.
- barrier-test simultaneous last-owner demote/remove/leave/transfer operations against real PostgreSQL and prove at least one Owner remains.
- translate only recognized expected violations into 409/semantic results.
- add optimistic concurrency tokens only where simultaneous edits have defined user-visible conflict behavior.
- write deterministic barrier-based PostgreSQL concurrency tests for important races; do not claim safety from sequential unit tests.

### Time

- persist instants as UTC `timestamptz`/DateTimeOffset semantics.
- persist local date/time and an IANA-compatible household time-zone ID for routines/events that depend on civil time.
- introduce a clock/time port when recurrence/deadline behavior needs deterministic tests, not merely to wrap `UtcNow` everywhere.

## Identity and authorization architecture

```text
React Native secure token storage
        |
        | opaque bearer access/refresh token
        v
ASP.NET Core authentication middleware
        |
        | ClaimsPrincipal / NameIdentifier
        v
Api current-user adapter exposes trusted actor UserId
        |
        v
Application loads/filters household membership
        |
        v
Domain invariant + resource permission
```

- use ASP.NET Core Identity and its EF stores/hashers.
- set `IdentityOptions.User.RequireUniqueEmail = true`, enforce a named unique filtered PostgreSQL NormalizedEmail index, and translate only that constraint's recognized `23505`; concurrent registration must yield one account and controlled responses.
- default beta choice for the first-party native client: Identity's built-in opaque bearer tokens, not custom JWTs. Expose exactly one auth mode: reject `useCookies=true`/avoid unchanged `MapIdentityApi`, remove the Phase 1 fake scheme from the real-Identity host, and record token lifetime, refresh, storage, and revocation behavior.
- normalize unknown-user, wrong-password, unconfirmed/not-allowed, and locked-out login failures through a thin Identity-backed boundary/filter to one public 401 shape while protected telemetry retains the reason.
- the client replaces its held refresh token with the token returned from refresh. Do not claim built-in one-time rotation, consumed-token replay detection, or per-device revocation; test/document old-token reuse until expiry/security-stamp change.
- cookies remain preferred for a browser-only same-site app; if Expo web uses cookies, add CSRF protection and an exact CORS/credential policy.
- do not put household roles into global Identity roles/long-lived claims. Membership can change and is scoped by HouseholdId.
- an endpoint-level `RequireAuthorization` is necessary but not sufficient. Application/resource queries must enforce membership for the requested object.
- use a consistent 404/403 policy that does not leak resource existence.
- add minimal Application `ICurrentUser` in Phase 1 and implement scoped Api `HttpCurrentUser` through `IHttpContextAccessor`; its `TryGetUserId`/nullable Guid contract exposes no HttpContext/ClaimsPrincipal and a missing/malformed configured subject produces a distinct unauthenticated outcome mapped to 401 with zero mutation. Phase 1 maps the route only in Testing with a real default test scheme; Phase 2 activates it under Identity. Health routes are explicitly anonymous under a fallback policy.
- before adding a HouseholdMember-to-Identity foreign key, choose and test one explicit Phase 1 data path: purge/reset pre-production databases, backfill matching users, or defer the FK. Test upgrading a database that already contains Phase 1 member rows.

## API boundary

- base product routes under `/api`; no `/v1` until incompatibility requires parallel support.
- Api request/response DTOs are separate from commands/results and Domain/EF/Identity entities.
- 201/200/204 for success as behavior dictates; 400 validation, 401 authentication, 403/404 authorization policy, 409 conflict, 429 rate limit, 500 safe unexpected error.
- all errors use RFC 7807 ProblemDetails plus a stable `code` and `traceId`; never stack traces or provider exception text.
- OpenAPI explicitly documents success and important error responses.
- mobile client IDs are opaque; never infer authorization from UI state.
- use pagination and filtering when a list can grow; avoid speculative generic query objects.
- establish a mobile compatibility/deprecation policy before public app-store distribution.

## Email and background work

Phase 3 invitation email can remain explicit:

```text
handler -> persist invitation -> commit -> email sender
                                      `-> failure leaves resendable invitation
```

Add a same-database outbox and an in-process `BackgroundService` only when the product requires automatic retry across process crashes. Requirements before adding it:

- a durable work table with status/attempt/next-at/last-error.
- idempotent email/notification consumer behavior.
- concurrency ownership for multiple app replicas.
- metrics/alerts/dead-letter handling.

A broker is still not justified merely because a worker exists.

## Observability architecture

Minimum production signal:

- liveness independent of dependencies.
- readiness checks PostgreSQL and any dependency required to serve traffic.
- structured request/use-case logs with deployment version, request/trace ID, status, duration, and safe pseudonymous actor/resource identifiers.
- counters/histograms for request duration/errors, authentication failures/lockouts/rate limits, database dependency duration/failure, email outcomes, and background backlog only if a worker exists.
- Application Insights/Azure Monitor ingestion in Phase 6.
- traces only when a request crosses meaningful external/background boundaries and logs/metrics cannot diagnose it.

Never log passwords, access/refresh/reset/invitation tokens, authorization headers, connection strings, full email bodies, or household free-text notes.

## Deployment topology

```text
GitHub Actions
   | restore/build/unit/integration/format/audit/scan
   | builds immutable image + migration bundle
   v
Azure Container Registry
   | immutable image promoted
   v
Isolated Staging and Production Container Apps environments
   |-- separate API apps, configuration, secrets, identities, and key scopes
   |-- managed HTTPS ingress; zero-traffic revisions before promotion
   |-- managed identity for ACR/Blob/Key Vault
   |-- shared Blob Data Protection key ring per environment
   |       `-- stable app name; versionless Key Vault wrapping-key ID
   |           with old wrapping-key versions retained
   |-- separate Azure Database for PostgreSQL Flexible Server/database
   |-- email provider configuration
   `-- Application Insights / Azure Monitor / Log Analytics

Container Apps Job or controlled private-network runner
   `-- backward-compatible expand migration with separate migration identity

Release order: build image/bundle -> additive schema -> backfill and prove
dual compatibility -> start/smoke zero-traffic revision -> shift and observe
-> intentionally close old-image rollback -> verified contract schema later.
PostgreSQL automated backups + point-in-time restore are configured and rehearsed.
```

Prefer separate Container Apps environments for staging/production isolation. An explicitly ephemeral staging design is acceptable only if it has separate data, secrets, identities, configuration, and key scopes.

The shared Data Protection key ring uses Blob Storage as the repository, a stable application name, and a versionless Key Vault key identifier for protection. Retain old wrapping-key versions while old protected payloads may be needed, and test authentication/confirmation/reset continuity across restart, revision replacement, and rotation; Key Vault alone is not the key-ring repository.

Do not lock Domain/Application to Azure. Infrastructure adapters and deployment configuration are the only Azure-aware areas.

## Dependency and boundary enforcement

Keep current project-reference tests and strengthen them over time:

- Domain references no outer HomePlatform assembly and no EF/ASP.NET/Npgsql/Identity package.
- Application references no Infrastructure/Api assembly.
- Api does not become a repository/data-access layer.
- feature-boundary tests are added only after multiple modules exist and a real accidental dependency is observed.

Architecture tests complement, not replace, csproj review and code review. A referenced but currently unused package may not appear in `Assembly.GetReferencedAssemblies`, so inspect project files in CI/review.

## Architectural decision gates

### Do not decide/build now

| Candidate | Default | Revisit only when |
|---|---|---|
| Microservices | no | independent deploy/team/scaling need exceeds distributed-system cost |
| Module-per-project | no | repeated cross-module references cannot be contained by folders/tests/review |
| Multiple databases/schemas | no | ownership/migration isolation has a measured benefit |
| Kafka/RabbitMQ/MassTransit | no | durable cross-process delivery/independent scaling is required |
| Event sourcing | no | complete historical state reconstruction is a core legal/business requirement |
| Domain events | no | one committed fact has multiple independent reactions and direct orchestration is coupled |
| Domain service | no | a stateless business rule spans types and fits no entity/value object |
| CQRS framework/MediatR | no | direct handlers have repeated pipeline needs and the framework reduces, rather than hides, complexity |
| Generic repository | no | no expected trigger; repositories represent aggregate/use-case persistence, not tables |
| UnitOfWork wrapper | no | a real multi-repository atomic use case cannot be expressed through one Infrastructure transaction port |
| AutoMapper | no | repetitive mechanical mapping is measured and compile-time explicit mapping no longer pays |
| FluentValidation | no | repeated cross-endpoint validation composition justifies a dependency |
| Redis | no | measured hot-read/database bottleneck plus safe invalidation design |
| WebSockets | no | validated sub-second collaboration; polling/refetch fails |
| Background queue | no | work must run/retry durably without a request |
| Kubernetes | no | Container Apps/App Service operational limits are proven |
| Multi-region | no | explicit RTO/RPO, global latency, usage, and budget require it |
| API versioning | no | an incompatible released client must coexist |

### ADRs to add when the decisions become active

1. Household role vocabulary/storage and owner invariant — Phase 1.
2. Identity user versus Domain profile ownership — Phase 2.
3. Native bearer versus browser cookie/session/revocation semantics — Phase 2.
4. household authorization matrix and 403/404 policy — Phase 3.
5. time zone/recurrence/missed-occurrence semantics — Phase 4.
6. Azure hosting/migration/backup topology — Phase 6.

## Architectural success checklist

- [ ] current solution builds and every test project actually executes.
- [ ] four-project dependency direction remains intact.
- [ ] each important write has an explicit handler, aggregate invariant, repository transaction, migration, endpoint DTO, and PostgreSQL integration test.
- [ ] Domain exposes no public mutation solely for EF.
- [ ] account identity is framework-secure and household authorization is resource-aware.
- [ ] no request can choose its actor identity.
- [ ] public HTTP never serializes Domain/EF/Identity objects.
- [ ] migrations apply from zero and are deployed explicitly.
- [ ] critical races are protected by behavior, database constraints/concurrency tokens, and barrier-based PostgreSQL tests.
- [ ] invitation acceptance commits invitation and Household mutations once or rolls both back.
- [ ] staging/production isolation and Blob-persisted, Key-Vault-protected Data Protection continuity are proven.
- [ ] CI/CD, container, staging, logs, alerts, least privilege, backup, restore, and rollback are proven.
- [ ] no deferred technology was introduced without its documented trigger.
- [ ] another .NET developer can find a feature, its rules, its endpoint, its mapping, and its tests without oral history.

## Platform references

- [ASP.NET Core resource-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0)
- [ASP.NET Core Identity API for first-party clients](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0)
- [ASP.NET Core Data Protection key storage providers](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0)
- [ASP.NET Core Data Protection Key Vault rotation guidance](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0)
- [EF Core backing fields](https://learn.microsoft.com/en-us/ef/core/modeling/backing-field)
- [EF Core entity constructors](https://learn.microsoft.com/en-us/ef/core/modeling/constructors)
- [EF Core migration deployment strategies](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
- [Azure Container Apps](https://learn.microsoft.com/en-us/azure/container-apps/overview)
- [Azure Database for PostgreSQL Flexible Server](https://learn.microsoft.com/en-us/azure/postgresql/flexible-server/service-overview)
