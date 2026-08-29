# HomePlatform Target Architecture

Status: **Authoritative target**  
Last reviewed: **2026-08-29**  
Decision boundary: accepted ADRs are binding; ADR 0006 remains **Proposed**

## Purpose and authority

This document is the single description of the intended technical
architecture. It does not approve proposed ADRs and it does not claim planned
components exist. The [context map](CONTEXT-MAP.md) owns business-language
boundaries, the [domain model](DOMAIN-MODEL.md) owns DDD terminology, and the
[roadmap](../roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md) owns sequencing.

## Current implemented state

At the inspected 2026-08-29 working-tree snapshot:

- the solution has four production projects with the intended dependency
  direction;
- API exposes health/readiness and Development OpenAPI;
- Infrastructure registers an empty EF Core DbContext and PostgreSQL provider;
- Domain contains early Household, HouseholdMember, roles, User, and Result
  code; concurrent uncommitted edits now prototype MembershipId/AccountId;
- Application contains an incomplete CreateHousehold handler and repository
  port;
- untracked Household mapping placeholders are empty and the repository is not
  registered;
- no product endpoint, authentication, resource authorization, migration,
  active household mapping, CI/CD, deployed environment, or frontend exists;
- the edited Household constructor now accepts an Owner AccountId and creates a
  Membership, while the handler and tests still use previous signatures. This
  documentation task did not claim a current green build, and source edits do
  not accept ADR 0006.

That is an early layered foundation, not a completed DDD implementation.

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

One API container and one PostgreSQL database are deployed per isolated
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
  boundary-neutral results.
- Infrastructure implements ports and owns EF Core, Npgsql, ASP.NET Core
  Identity persistence, email, clocks/providers, and migrations.
- API owns HTTP request/response types, authentication-principal mapping,
  middleware, routing, error mapping, and composition.
- Domain never references ASP.NET Core, EF Core, Npgsql, Identity, logging,
  provider SDKs, or Azure.

## Module ownership

The target business boundaries are summarized in the
[authoritative context map](CONTEXT-MAP.md). They start as feature folders and
namespaces inside the existing assemblies. Do not create a project per context.

The proposed minimum is Identity & Access, Households, Tasks & Routines,
Shopping, Events, and a read-only Today composition. Notifications and Calendar
Integration begin as supporting modules only when a concrete use case triggers
them.

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
- one aggregate write normally commits once;
- expected errors use stable Application outcomes; unexpected exceptions remain
  generic at the HTTP boundary.

## Read path

```text
authorized Application query
  -> Infrastructure no-tracking projection
  -> Application read DTO
  -> API response DTO
```

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

## Account and Membership decision gate

[ADR 0006](../adr/0006-separate-account-and-household-membership-identity.md)
proposes:

- `AccountId` as trusted credential-bearing identity;
- stable `MembershipId` as participation identity within a Household;
- optional later-verifiable `AccountId` on a Membership;
- several Household Memberships for one Account;
- authorization roles separate from family relationships;
- assignment and retained history by `MembershipId`.

This is target direction, not an accepted or implemented fact. No durable
membership mapping or migration may be created until the ADR is explicitly
accepted or rejected and this document is updated. A global Person context and
a general capability engine remain unjustified.

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

If ADR 0006 is accepted, persistence must support stable Membership identity,
nullable Account linking, and scoped uniqueness for a linked Account within one
Household. Exact keys, inactive-membership filtering, and cross-module FK
strategy remain implementation decisions.

## Identity and authorization

```text
ASP.NET Core authentication
  -> trusted AccountId
  -> resolve current Membership for HouseholdId
  -> apply role/resource policy
  -> execute authorized use case
```

- ASP.NET Core Identity belongs in Infrastructure, not Domain.
- Household roles are not global Identity roles or long-lived claims.
- `RequireAuthorization()` proves authentication, not resource access.
- Every object/Household identifier is authorized server-side.
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
- Use RFC 7807 ProblemDetails with stable `code` and `traceId`.
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

The six-month target is one immutable API container on Azure Container Apps and
Azure Database for PostgreSQL Flexible Server, with isolated staging/production
data, identities, secrets, and configuration. CI builds/tests/scans once; CD
promotes the same artifact with explicit migration and rollback gates.

Before public beta, prove least privilege, secret rotation, shared Data
Protection key continuity, health, structured logs, metrics/alerts, backup,
restore, export/deletion behavior, rollback, and incident ownership.

## Explicit non-goals until triggered

| Candidate | Revisit only when |
|---|---|
| Microservices or multiple databases | independent deployment/team/scale need exceeds distributed-system cost |
| MediatR/CQRS framework | repeated pipeline behavior makes it simpler than direct handlers |
| Generic repository or UnitOfWork wrapper | a proven use case cannot express persistence clearly through focused ports |
| Domain events/outbox/broker | one committed fact requires durable independent reactions |
| Redis | measured database bottleneck plus safe invalidation design |
| WebSockets | validated sub-second need and refetch/polling is insufficient |
| Event sourcing | historical reconstruction is a core business/legal requirement |
| Kubernetes or multi-region | explicit scale, availability, RTO/RPO, and budget require it |
| Global Person/Profile | proven cross-household identity lifecycle and ownership |
| General capability engine | repeated permissions cannot be expressed by the role/resource matrix |

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
