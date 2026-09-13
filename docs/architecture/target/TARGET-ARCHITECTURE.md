# HomePlatform Target Architecture

Status: **Authoritative target**  
Last reviewed: **2026-09-13**
Decision boundary: accepted ADRs, including ADR 0006, are binding

## Purpose and authority

This document is the single description of the intended technical
architecture. It does not approve proposed ADRs and it does not claim planned
components exist. The [context map](CONTEXT-MAP.md) owns business-language
boundaries, the [domain model](DOMAIN-MODEL.md) owns DDD terminology, and the
[roadmap](../roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md) owns sequencing.

## Current architecture

As of source inspection on 2026-09-13 at `main@07eeb12`, with bearer sign-in
committed in `4180096` and Refresh in `635d181`:

- four production projects follow the dependency graph below;
- Domain contains Household and its HouseholdMember entities, HouseholdRole,
  and a small Result helper. It has no User, Account aggregate, explicit value
  objects, Domain Services, or Domain Events;
- Application contains CreateHousehold, RegisterAccount, SignInAccount,
  RefreshAccount, their input/results, and focused ports. Accounts uses
  `Accounts/<UseCase>/` with Register, SignIn, and Refresh use cases and contracts
  local to each operation;
- Infrastructure owns the focused Household repository, Npgsql DbContext,
  field-backed mappings, roleless ASP.NET Core Identity persistence, and the
  UserManager-backed registration and SignInManager-backed sign-in/refresh adapters;
- API maps health/readiness, Development/Testing OpenAPI, Testing-only
  authenticated `POST /api/households`, and anonymous
  `POST /api/accounts/register`, `POST /api/accounts/sign-in`, and
  `POST /api/accounts/refresh` in every environment, including Production;
- three migrations exist: InitialHousehold, LimitHouseholdNameLength, and
  AddIdentityPersistence. The local dotnet-ef pin is 10.0.11;
- registration, bearer sign-in, and Refresh are implemented. Refresh validates
  expiry/security stamp and issues new access/refresh tokens through the framework
  bearer handler; the endpoint returns Results.Empty after that response.
  Permanent real-bearer PostgreSQL tests prove renewal, safe failures, and the
  registered AccountId on a protected Household write. Current verification is
  105/105 tests; the historical registration baseline remains 72/72.
  Confirmation/recovery/revocation, rate limiting, Account-reference validation,
  Household resource authorization, deployment, and frontend remain incomplete.

The [DDD/Clean Architecture audit](../DDD-ARCHITECTURE-AUDIT.md) records the
historical verification and findings, with a registration completion follow-up. The earlier 57-test Phase 1
result is dated evidence, not a claim about the current registration changes.

### Current DDD status

HomePlatform uses tactical DDD in the small Household aggregate: independent
Membership identity, protected member creation, initial Owner, and scoped
duplicate-link invariants. Handlers, DTOs, DI, and four assemblies are application
architecture, not additional DDD patterns. Identity is a framework-owned
supporting capability. Multiple implemented bounded contexts or strategic DDD
are not established by this snapshot.

## Target state

HomePlatform remains one pragmatic modular monolith:

```text
React Native / Expo client
            |
            | HTTPS and authenticated requests
            v
+-----------------------------+
| HomePlatform.Api            |
| HTTP DTOs, auth principal,  |
| ProblemDetails, composition |
+-------------+---------------+
              |
              v
+-----------------------------+
| HomePlatform.Application    |
| explicit commands/queries,  |
| orchestration, authz, ports |
+-------------+---------------+
              |
              v
+-----------------------------+
| HomePlatform.Domain         |
| entities, value objects,    |
| aggregates, invariants      |
+-------------+---------------+
              ^
              |
+-------------+---------------+
| HomePlatform.Infrastructure |
| EF/Npgsql, Identity, email, |
| provider adapters, queries  |
+-------------+---------------+
              |
              v
          PostgreSQL
```

The deployment target is one API container and one PostgreSQL database per isolated
environment. Modules are business boundaries inside the four projects; they
are not separate services or databases.

## Dependency rules

```text
Domain         -> no HomePlatform or technical project
Application    -> Domain
Infrastructure -> Application + Domain
Api            -> Application + Infrastructure
```

- Domain contains business state and behavior only.
- Application contains use cases, authorization decisions, ports, and
  boundary-neutral results. Its only current technical package is
  `Microsoft.Extensions.DependencyInjection.Abstractions` for AddApplication;
  this pragmatic composition helper does not permit HTTP, EF, or Identity use.
- Infrastructure implements ports and owns EF Core, Npgsql, ASP.NET Core
  Identity persistence, email, clocks/providers, and migrations.
- API owns HTTP request/response types, authentication-principal mapping,
  middleware, routing, error mapping, and composition. The current `/ready`
  probe directly uses DbContext for connectivity; this is a narrow operational
  adapter exception, not a precedent for product data access in endpoints.
- Domain never references ASP.NET Core, EF Core, Npgsql, Identity, logging,
  provider SDKs, or Azure.

## Module ownership

The target business boundaries are summarized in the
[authoritative context map](CONTEXT-MAP.md). They start as feature folders and
namespaces inside the existing assemblies. Do not create a project per context.

Candidate feature areas are Identity & Access, Households, Tasks & Routines,
Shopping, optional Events, and a read-only Today composition. They do not each
require a separate bounded context; apply the context-map promotion criteria.
Notifications and Calendar Integration begin as supporting modules only when a
concrete use case triggers them.

## Write path

```text
HTTP request DTO
  -> authenticated Account context
  -> Application command/handler
  -> resource authorization
  -> Domain aggregate behavior
  -> focused repository/transaction port
  -> one EF Core SaveChanges transaction
  -> explicit HTTP response DTO or ProblemDetails
```

Rules:

- request DTOs contain user-editable input only;
- clients never select the authenticated actor;
- handlers are explicit classes; no mediator is required;
- repositories speak aggregate/use-case persistence, not generic CRUD;
- one aggregate write normally commits once. The current repository AddAsync
  includes the durable commit; it does not merely stage a new entity;
- expected errors use stable Application outcomes; unexpected exceptions remain
  generic at the HTTP boundary.

For invitation acceptance, use one focused transaction boundary over the
Invitation and Household changes if both must succeed together. Do not compose
two auto-committing repository calls or imply that Identity registration and
Household creation are currently one transaction. EF DbContext already tracks
the unit of work; this use case does not justify a generic UnitOfWork wrapper.

## Read path

```text
authorized Application query
  -> Infrastructure no-tracking projection
  -> Application read DTO
  -> API response DTO
```

This is a future read path. Plain command/query separation on the same database
does not require a CQRS framework, asynchronous projection, or separate datastore.

Queries need not hydrate aggregates. Today composes authorized projections and
causes no writes. A read never materializes routine occurrences as a side
effect.

## Domain modelling rules

For each behavior:

1. state the user/use-case need;
2. identify the invariant and owner;
3. add the minimum entity/value object/state;
4. prove Domain behavior;
5. add the Application use case and tests;
6. add ports only for real boundaries;
7. implement mapping, migration, and transaction semantics;
8. expose explicit API DTOs;
9. prove the flow against real PostgreSQL;
10. stop before the next behavior.

Do not pre-create base entities, aggregate-root frameworks, generic
repositories, events, or empty modules.

## Accepted Account and Membership boundary

[ADR 0006](../adr/0006-separate-account-and-household-membership-identity.md)
was Accepted on 2026-08-30. Its core identity model is implemented:

- AccountId is a Guid reference to credentials owned by Identity;
- MembershipId is the stable identity of participation inside one Household;
- AccountId is optional on Membership; one Account may participate in several
  Households, and loginless non-Owner members need no invented credentials;
- Owner/Member/Guest are Household roles, never global Identity roles.

ApplicationUser inherits `IdentityUser<Guid>` in Infrastructure. IAccountRegistration
returns an Application-owned result, and ICurrentAccount supplies a trusted actor
through the API adapter. There is no reason to invent an Account aggregate
without independent product-domain behavior. Email/password may cross the
registration use-case boundary transiently; they do not belong in Household Domain.

Verified linking/unlinking, Account existence/lifecycle integrity, last-Owner
concurrency, and assignment/history preservation remain follow-up work. A shared
database alone does not validate an Account reference. No global Person context
or general capability engine is justified.

### Adopted Account deletion and ownership lifecycle

[DELETION-DESIGN.md](../../privacy/DELETION-DESIGN.md) is canonical for
DeleteAccount, LeaveHousehold, TransferOwnership and CloseHousehold. All four
are NOT YET IMPLEMENTED. Account deletion resolves every Household membership;
it is not implicit Household deletion. Membership never implies ownership or
automatic promotion. A continuing Household must retain an Account-linked
Owner; its departing last Owner explicitly transfers to a concrete eligible
Account-linked person or explicitly closes the Household.

Loginless people can still have personal data. Shared Household/domain data
and data about other people are not owned by one Account because it has the
Owner role. **ADOPTED:** DeleteAccount explicitly deletes all Account-linked
memberships after ownership resolution, then Identity/ApplicationUser and
Account-owned data. No silent loginless conversion occurs; AccountId remains
nullable for separate loginless-member flows. Stable MembershipId is not a
blanket retention rule. Concrete future attribution/history remains OPEN.

Every persisted feature must complete the
[lifecycle checklist](../../privacy/DELETION-DESIGN.md#feature-lifecycle-checklist):
scope, personal references, leave/deletion/closure, surviving/person-dependent
records, attribution UI and export/retention/privacy impact. Feature/data-type
rules decide shared-record survival, not a generic runtime usage heuristic.
Remove unnecessary personal references, delete person-dependent data without a
continuing purpose, and do not equate null references with anonymization.

DeleteAccount requires one focused atomic boundary over the approved Household
lifecycle changes and Identity deletion across all memberships. Do not compose
independently committed deletions. Exact transaction/concurrency mechanics and
EF APIs remain OPEN implementation details; no generic UnitOfWork or privacy
service is justified.

## Persistence

- EF configurations and migrations live in Infrastructure.
- Domain remains persistence-ignorant; private backing fields/constructors are
  allowed only to preserve invariant-safe materialization.
- Explicitly configure keys, lengths, conversions, relationships, deletion,
  uniqueness, checks, indexes, and concurrency.
- Pin `dotnet-ef` in a repository-local tool manifest.
- Prove migrations from an empty real PostgreSQL database and check pending
  model changes in CI.
- API startup never migrates production automatically.
- Prefer additive, backward-compatible migrations once data exists.
- One DbContext/database/schema is the default until measured ownership or
  migration pressure justifies change.

Current persistence uses MembershipId as the member primary key, a required
HouseholdId foreign key with cascade delete, and a unique unfiltered index on
(HouseholdId, AccountId). PostgreSQL permits multiple null AccountIds. No active
membership flag/filter or Account foreign key exists. **ADOPTED, NOT YET
IMPLEMENTED:** add a nullable HouseholdMember.AccountId FK to AspNetUsers.Id
with rejecting deletion behavior. DeleteAccount explicitly removes all linked
memberships after ownership resolution and before Identity deletion; no
Account-to-HouseholdMember CASCADE DELETE or
automatic SET NULL. Exact EF DeleteBehavior API and PostgreSQL mapping remain
OPEN implementation details to verify. The existing Household-to-member
cascade is a different relationship and does not authorize implicit closure.
Future inactive-membership semantics remain OPEN.

Database guarantees are narrower than all Domain invariants: name nullability
and length and linked-account uniqueness are mapped; nonblank names, valid role
values, Owner Account presence, and at least one Owner have no database checks.
This does not expose public mutation today; review final guards when adding
alternate write paths. EF materialization does not rerun the public constructor.

## Identity and authorization

```text
ASP.NET Core authentication
  -> trusted AccountId
  -> check current Account existence/validity for protected product requests
  -> resolve current Membership for HouseholdId
  -> apply role/resource policy
  -> execute authorized use case
```

- ASP.NET Core Identity belongs in Infrastructure, not Domain.
- Household roles are not global Identity roles or long-lived claims.
- `RequireAuthorization()` proves authentication, not resource access.
- Every object/Household identifier is authorized server-side.
- ADOPTED, NOT YET IMPLEMENTED: after successfully committed DeleteAccount,
  deny new protected product requests even with an unexpired access token.
  Current opaque access validation does not reload Account state; add focused
  current-Account validation alongside resource membership checks. Exact token
  lifetime is OPEN and cannot replace this requirement.
- Missing or malformed subject identity produces 401 and zero mutation.
- Use one documented first-party authentication mode; do not invent custom
  password/token cryptography.
- Use a consistent 403/404 policy that avoids unnecessary resource disclosure.

The [security roadmap](../roadmap/SECURITY-ROADMAP.md) is authoritative for the
detailed trust model and release gates.

## API boundary

- Product routes live under `/api`; version only when incompatible clients must
  coexist.
- API DTOs are distinct from Domain, EF, Identity, command, and result types.
- Target a consistent ProblemDetails contract with stable `code` and `traceId`.
  Current Household validation uses ProblemDetails and 401 is empty; registration
  returns ProblemDetails with an `errors` array of Application-owned codes. Its
  generated OpenAPI declares 201 RegisterAccountResponse and 400 ProblemDetails;
  unexpected database failures remain generic 500. Sign-in and Refresh declare
  200 AccessTokenResponse and 400/401 ProblemDetails. Refresh's generated contract
  has no 201 or extra success status; its HTTP failures expose RefreshTokenRequired
  or InvalidRefreshToken in `errors`. Cross-endpoint code/traceId consistency
  remains a follow-up.
- Document meaningful 2xx/400/401/403-or-404/409/429 responses.
- Never serialize Domain/EF/Identity entities.
- Establish mobile compatibility/deprecation policy before public app-store
  distribution.

## Testing and concurrency

- Domain tests prove invariants without frameworks.
- Application tests use hand-written fakes where small and clear.
- Integration tests use PostgreSQL/Testcontainers, never SQLite or EF InMemory
  as persistence proof.
- Database constraints are final guards for uniqueness.
- Last-Owner, invitation acceptance, account linking, recurrence identity, and
  concurrent edits require deterministic barrier-based PostgreSQL tests.
- A sequential unit test, UUID, or realtime transport is not concurrency proof.

## Deployment target

One immutable API container and PostgreSQL with isolated staging/production
data, identities, secrets and configuration remain the architecture direction.
Hosting, providers and regions are **OPEN**. The earlier Azure Container Apps /
Azure Database for PostgreSQL Flexible Server design is **PROPOSED**, not an
adopted provider decision or evidence of deployment. CI builds/tests/scans once;
CD promotes the same artifact with explicit migration and rollback gates.

Before public beta, prove least privilege, secret rotation, shared Data
Protection key continuity, health, structured logs, metrics/alerts, backup,
restore, export/deletion behavior, rollback, and incident ownership.

## Evolution policy: Now, Next, Later if needed

Patterns follow a concrete problem and the simplest adequate solution. A phase
number, future feature name, or portfolio goal does not trigger infrastructure.

| Timing | Candidate | Problem/trigger and simplest first response |
|---|---|---|
| Now | Four layers, explicit handlers, aggregate repository, one DbContext | Keep the existing use-case, Domain, and persistence separation. No structural rewrite. |
| Next | Account-reference integrity, required Household lifecycle, current-Account validation, then DeleteAccount | Follow NEXT-STEPS and the deletion design. Dependency guards and confirmation/recovery/revocation remain gates; no new generic privacy/session framework. |
| Next, with first read | Queries/read DTOs | Project authorized data from the same PostgreSQL database; do not hydrate aggregates for display. |
| Later if needed | Bounded context split | Independent language, invariants, model, lifecycle, ownership, and reasons to change are demonstrated; start with folders/contracts. |
| Later if needed | Domain events | One domain action has multiple independent reactions and direct orchestration becomes coupled. Start in process and decide before/after-commit semantics. |
| Later if needed | Integration events | Another module/process must react asynchronously to a committed fact. Define a stable contract, retry, and idempotency; this alone needs no broker. |
| Later if needed | Outbox | A database change and reliable external publication/delivery must survive a crash atomically. Store intent in the same transaction, then retry with deduplication. It does not guarantee exactly-once delivery. |
| Later if needed | Worker/delivery table | A concrete reminder must run without a request. Start with a small database-backed worker. |
| Later if needed | Message bus/broker | Cross-process delivery, independent consumers, throughput, or operations make the database-backed worker inadequate. Budget redelivery and operations explicitly. |
| Later if needed | Calendar anti-corruption adapter | An approved provider has different identifiers/time/lifecycle semantics; translate in an ordinary Infrastructure adapter first. |
| Later if needed | MediatR | Repeated cross-cutting handler behavior demonstrably becomes simpler than direct composition. It is independent of CQRS and DDD. |
| Later if needed | Separate CQRS datastore/materialized projections | Measured read workload cannot be handled by indexed same-database projections; accept lag, rebuild, and synchronization costs explicitly. |
| Not planned | Generic repository / generic UnitOfWork wrapper | Current focused ports and EF already cover persistence. A multi-aggregate use case gets one focused transaction, not a generic CRUD framework. |
| Not planned | Specification framework / Shared Kernel | No repeated complex query policy or independently owned contexts sharing a governed model exists. Use focused queries and local primitives. |
| Not planned | Event sourcing | First try audit/history records. Reconsider only if events must be the authoritative state and full replay is essential enough to justify versioning, rebuild, and deletion complexity. |
| Not planned | Sagas | No distributed multi-step business transaction exists. Use a local transaction now; compensation is relevant only after independent processes actually require it. |
| Not planned | Microservices / multiple databases | Reconsider only when independent deployment, scaling, or ownership requirements outweigh distributed-system cost. Modular monolith remains preferred. |
| Later if needed | Redis / WebSockets | Measured query or sub-second UX need defeats indexing/projection or refetch/polling, with invalidation/conflict semantics defined. |
| Not planned | Kubernetes / multi-region | Requires explicit availability, recovery, scale, and operating-budget evidence. |
| Not planned | Global Person/Profile / general capability engine | Requires independent cross-household identity lifecycle or permissions that the role/resource matrix cannot express. |

## Success evidence

The target is not achieved until:

- the solution and every test project are green;
- the four-project dependency direction remains enforced;
- each important write has explicit handler, invariant, transaction, migration,
  endpoint DTO, and PostgreSQL proof;
- actor identity is trusted and Household authorization is resource-aware;
- migrations apply from zero without drift;
- critical races have database/concurrency proof;
- staging/production isolation, deployment, observability, backup/restore, and
  rollback are exercised;
- no deferred technology was introduced without its trigger;
- another senior .NET developer can trace a feature without oral history.
