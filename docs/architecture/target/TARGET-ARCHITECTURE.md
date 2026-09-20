# HomePlatform Target Architecture

Status: **Authoritative target**  
Last reviewed: **2026-09-20**
Decision boundary: accepted ADRs, including ADR 0007, are binding; acceptance is not implementation

## Purpose and authority

This document is the single description of the intended technical
architecture. It does not approve proposed ADRs and it does not claim planned
components exist. The [context map](CONTEXT-MAP.md) owns business-language
boundaries, the [domain model](DOMAIN-MODEL.md) owns DDD terminology, and the
[NEXT-STEPS](../roadmap/NEXT-STEPS.md) alone owns sequencing. HomePlatform
helps people coordinate shared life across households, relationships and changing
family structures. Product/domain slices lead development; deeper hardening is
pulled forward for concrete risk, feature dependencies or real-user release.

## Accepted domain direction — TARGET

[ADR 0007](../adr/0007-person-as-stable-human-identity.md) adds stable Person
identity separate from Account credentials and HouseholdMembership participation.
Relationship connects Persons independently of co-residence. A Person may have
no Account and participate in multiple Households where product rules permit.
Children and other family roles are contextual, not Person subtypes.
CareCircle/CareCircleMembership remain **FUTURE**, with no detailed care schema.

These concepts are absent from source. Design the smallest Account–Person link,
Person foundation and membership migration before implementing them; keep the
four-layer modular monolith and Identity in Infrastructure. The
[domain model](DOMAIN-MODEL.md#accepted-core-model--target) explains the concepts.

## Current architecture

Source baseline: committed `main@7162c35`, 2026-09-20. Household nonmember-404
authorization is committed and the affected 117-test slice passed locally.
Separate DeleteAccount endpoint/handler/Identity adapter/tests remain unchanged
and uncommitted. Their transaction/rollback correctness and completion are not
established by this Household test run.

- Four production projects follow the dependency graph below.
- Domain owns Household/HouseholdMember, roles, creation, explicit ownership
  transfer, non-Owner leave, Owner-authorized close and typed lifecycle results.
  It has no Identity/EF dependency, Account aggregate, Domain Service or events.
- Application owns RegisterAccount, SignInAccount, RefreshAccount,
  CreateHousehold, TransferOwnership, LeaveHousehold and CloseHousehold handlers,
  focused ports and results. API derives the actor through ICurrentAccount.
- Infrastructure owns roleless Identity, its registration/sign-in/refresh
  adapters, HomePlatformDbContext, mappings and HouseholdRepository. GetByIdAsync
  tracks the aggregate with Include(Members); update saves tracked changes and
  delete removes the aggregate before SaveChangesAsync. Each write method
  commits independently; no cross-use-case transaction coordinator is present.
- API configures AddProblemDetails/UseExceptionHandler, then authentication and
  authorization. Health/readiness are mapped; OpenAPI is Development/Testing-only.

| HTTP contract | Availability | Implemented behavior |
|---|---|---|
| POST /api/accounts/register | Anonymous, all environments | Registration via Identity; 201 response or validation ProblemDetails |
| POST /api/accounts/sign-in | Anonymous, all environments | Framework bearer tokens and Identity lockout |
| POST /api/accounts/refresh | Anonymous, all environments | Expiry/security-stamp validation and new access/refresh tokens |
| POST /api/households | Authenticated, Testing-only | Create with trusted initial Owner; 201/400/401 |
| PUT /api/households/{householdId}/ownership | Authenticated, Testing-only | Current Owner transfers to Account-linked target Membership; 204/400/401/403/404 |
| DELETE /api/households/{householdId}/membership | Authenticated, Testing-only | Member/Guest leaves; every Owner refused; 204/401/404/409 |
| DELETE /api/households/{householdId} | Authenticated, Testing-only | Owner closes by physical delete, membership cascade, Accounts preserved; 204/401/403/404 |

Household route IDs are constrained as Guids in endpoint mappings.

Source anchors: [Program](../../../backend/src/HomePlatform.Api/Program.cs),
[Household endpoints](../../../backend/src/HomePlatform.Api/Households/HouseholdEndpoints.cs),
[Domain behavior](../../../backend/src/HomePlatform.Domain/Household/Household.cs),
[repository](../../../backend/src/HomePlatform.Infrastructure/Persistence/Repositories/HouseholdRepository.cs),
[Application handlers](../../../backend/src/HomePlatform.Application/Households),
and [Identity adapters](../../../backend/src/HomePlatform.Infrastructure/Identity).

Four migrations exist: InitialHousehold, LimitHouseholdNameLength,
AddIdentityPersistence and AddHouseholdMemberAccountReference. The nullable
Account FK uses ClientNoAction / NO ACTION and has an AccountId lookup index.
The repository pins dotnet-ef 10.0.11.

Three test projects contain unit, dependency, real PostgreSQL/Testcontainers,
HTTP and generated OpenAPI coverage. The CI workflow defines build, test,
formatting, dependency audit and migration checks. Historical local runs are
listed in [NEXT-STEPS](../roadmap/NEXT-STEPS.md#verified-locally); test source and
workflow presence do not establish current runtime success, hosted CI or a
verified deployment.

**Not implemented in this main snapshot:** current-Account validity on each
protected request, Household optimistic concurrency, DeleteAccount, verified
link/unlink and invitations, paginated read projections, frontend, Tasks,
Shopping and Events. Authentication recovery/confirmation/revocation and
operational release controls are incomplete. Resource authorization is partial:
transfer/leave/close check loaded membership/role and conceal missing Households
and nonmember access with 404. Known non-Owner members receive 403 for
transfer/close; transfer validates the target only after actor authorization. No production Household exposure
is implied by these Testing-only operations.

### Current DDD status

HomePlatform uses tactical DDD in the small Household aggregate: independent
Membership identity, protected member creation, initial Owner, scoped
duplicate-link invariants and explicit transfer/leave/close rules. Handlers, DTOs, DI, and four assemblies are application
architecture, not additional DDD patterns. Identity is a framework-owned
supporting capability. Multiple implemented bounded contexts or strategic DDD
are not established by this snapshot.

## Future direction / Target state

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

Candidate feature areas are Identity & Access, People/Relationships, Households, Tasks & Routines,
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
- one aggregate write normally commits once. Current AddAsync, UpdateAsync and
  DeleteAsync each call SaveChangesAsync; they are not staging-only operations;
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

## AS-IS Account and Membership boundary and TARGET evolution

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

The optional Account FK now enforces non-null reference existence and rejects
unresolved Account deletion. Verified linking/unlinking workflows,
protected-request current-Account validation, last-Owner concurrency and future
assignment/history handling remain follow-up work. Transfer/leave/close exist
with the limited behavior described above. ADR 0007 now accepts Person as the
human identity and evolves Membership toward Person participation. Exact
Account–Person linking/persistence and module/aggregate boundaries remain open;
no general capability engine or Domain Account aggregate is justified.

### Adopted Account deletion and ownership lifecycle

[DELETION-DESIGN.md](../../privacy/DELETION-DESIGN.md) is canonical for
DeleteAccount, LeaveHousehold, TransferOwnership and CloseHousehold. The three
Household operations are IMPLEMENTED with Testing-only endpoints; DeleteAccount
is NOT IMPLEMENTED in committed main. Planned Account deletion resolves every Household membership;
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
membership flag/filter exists. **IMPLEMENTED; historical local verification on 2026-09-14:** nullable
HouseholdMember.AccountId references AspNetUsers.Id with EF ClientNoAction and
PostgreSQL NO ACTION. AddHouseholdMemberAccountReference adds the FK and a
separate AccountId index for cross-Household lookup. Tests prove valid/null
links, missing-Account rejection and tracked/untracked deletion guards, plus
valid-data upgrades and failure on dangling historical links without silent
cleanup. Actual environment data/migration state remains NOT VERIFIED.

DeleteAccount remains NOT IMPLEMENTED in committed main: it must explicitly remove all linked
memberships after ownership resolution and before Identity deletion, without
Account-to-HouseholdMember CASCADE DELETE or automatic SET NULL. The existing
Household-to-member cascade is a different relationship and does not authorize
implicit closure. Future inactive-membership semantics remain OPEN.

Database guarantees are narrower than all Domain invariants: name nullability
and length and linked-account uniqueness are mapped; nonblank names, valid role
values, Owner Account presence, and at least one Owner have no database checks.
This does not expose public mutation today; review final guards when adding
alternate write paths. EF materialization does not rerun the public constructor.

## Identity and authorization target

```text
ASP.NET Core authentication
  -> trusted AccountId
  -> optional future current-Account validation (deferred hardening)
  -> resolve current Membership for HouseholdId
  -> apply role/resource policy
  -> execute authorized use case
```

- ASP.NET Core Identity belongs in Infrastructure, not Domain.
- Household roles are not global Identity roles or long-lived claims.
- `RequireAuthorization()` proves authentication, not resource access.
- Every object/Household identifier is authorized server-side.
- DEFERRED TECHNICAL DEBT: an already-issued short-lived access token may
  remain usable until expiry after Account deletion without immediate server-side
  revocation/current-Account validation. Exact lifetime and the acceptable
  release window remain OPEN. Refresh must separately reject deleted/invalid
  Accounts; token authentication never substitutes for resource authorization.
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

## Testing and concurrency requirements

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
| Next, per NEXT-STEPS | Minimal Person foundation and membership evolution | Design link/migration/lifecycle first, preserve current behavior, then prove Person independent of Account through a concrete flow. |
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
| Deferred hardening | Current-Account validity, Household concurrency, DeleteAccount completion | Triggers and evidence live in the debt register/security/deletion design; not blanket prerequisites to product development. |
| Not planned | General capability engine | Reconsider only if concrete permissions cannot be expressed by focused role/resource policy. |

## Product and release evidence

Incremental product success means a stable human identity without forced login,
separate participation in each Household, and real flows validating those
boundaries. Each slice needs proportionate correctness and authorization proof.
DI reviews, SQL-plan analysis and broad test-strategy consolidation follow real
implementation needs; they do not all precede Person development.

**RELEASE GATE:** production readiness additionally requires:

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
