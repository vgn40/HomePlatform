# HomePlatform Six-Month Masterplan

Status: **Authoritative strategic roadmap**  
Last reviewed: **2026-08-29**  
Planning horizon: approximately 26 weeks

## Executive direction

Deliver a small, secure household-coordination beta through one vertical slice
at a time. Keep the accepted four-project modular monolith and avoid speculative
frameworks or distributed infrastructure. The six-month outcome is not a broad
family super-app; it is a coherent product whose identity, authorization,
transactions, time behavior, tests, and operations can be defended.

The current phase is **Phase 1 — settle membership identity and complete the
CreateHousehold slice**. The first durable membership schema is blocked until
[ADR 0006](../adr/0006-separate-account-and-household-membership-identity.md)
is explicitly accepted or rejected.

For the next executable actions, use [NEXT-STEPS.md](NEXT-STEPS.md). It is the
only current implementation-order document.

## Current state

The repository has a .NET 10 layered foundation, health/readiness, PostgreSQL
connectivity, early Household code, and an incomplete CreateHousehold slice. It
does not have a product endpoint, authentication, active household persistence,
migrations, CI/CD, a deployed environment, or a frontend.

Source inspection on 2026-08-29 found that the edited Household constructor,
handler, and Domain tests are not aligned. The previous role-vocabulary
compilation diagnosis is historical; this documentation task did not establish
a fresh green build.

## Target beta scope

In scope:

- Account lifecycle through ASP.NET Core Identity;
- trusted authenticated actor context;
- Household creation, Membership, invitations, roles, and resource
  authorization;
- tasks and deliberately bounded recurring routines;
- shared shopping;
- a side-effect-free Today view;
- basic internal events only if earlier gates and capacity permit;
- PostgreSQL migrations and concurrency proof;
- CI/CD, observability, security hardening, export/deletion behavior, and one
  reproducible Azure beta environment.

Out of scope unless an explicit gate changes it:

- global Person/Profile graph or child/caregiver product area;
- granular permission platform;
- external calendar implementation;
- push/realtime platform;
- AI ingestion, meals, expenses, maintenance, location, rewards, or hardware;
- microservices, brokers, event sourcing, Redis, Kubernetes, and multi-region.

## Cross-phase rules

1. Current and target state stay visibly separate.
2. Actor identity is server-derived; no request/command chooses it.
3. Each write traces API -> Application -> Domain -> focused port -> one commit.
4. Persistence and concurrency claims use real PostgreSQL/Testcontainers.
5. No phase starts before the prior exit gate passes.
6. A proposed ADR is a stop-gate, not implementation authority.
7. Scope is reduced before quality, security, or data-integrity gates.

## Phase 1 — Membership decision and CreateHousehold

Goal: produce one secure, PostgreSQL-backed CreateHousehold vertical slice.

### Required decisions

- Accept or reject ADR 0006 before mapping/migration.
- Distinguish authorization role from family relationship.
- Define trusted Account actor -> initial Owner Membership behavior.
- Preserve stable Household participation/history according to the approved
  identity decision.

### Delivery

- restore constructor/caller/test alignment and a green solution;
- protect Household name, owner-at-creation, duplicate Membership, and
  collection encapsulation invariants;
- remove caller-selected `CreatorUserId` from the use-case boundary;
- add a narrow Application current-account port and hand-written handler tests;
- complete EF mappings, repository registration, and one reviewed migration;
- expose an authenticated Testing-only HTTP route with explicit DTOs and
  ProblemDetails;
- prove save/reload, zero-row failure, actor trust, and migration-from-zero on
  PostgreSQL;
- add backend CI and exact format/migration gates.

### Exit gate

Build and every test project are green; the accepted/rejected identity decision
matches Domain, Application, mapping, and migration; exactly one Household with
one Owner Membership is committed for the trusted actor; Production does not
expose a fake-auth product route.

## Phase 2 — Identity and trusted Account lifecycle

Goal: replace test authentication with a supported first-party Identity flow.

### Delivery

- add ASP.NET Core Identity in Infrastructure with Guid Account identity;
- choose and record one native bearer or browser-cookie mode;
- implement registration, confirmation, sign-in, refresh/session, recovery,
  revocation, and normalized-email uniqueness behavior;
- decide the fate of the current Domain `User`; do not duplicate credentials;
- implement the approved Account-to-Membership link semantics;
- prove migration upgrades from the Phase 1 schema and data strategy;
- normalize public auth failures while preserving protected telemetry.

### Exit gate

Real authentication establishes the actor; Account lifecycle tests pass;
duplicate normalized email has one controlled winner; no global Identity role
is used for Household authority; fake authentication is absent outside tests.

## Phase 3 — Household collaboration and authorization

Goal: make multi-person Household collaboration secure and lifecycle-complete.

### Delivery

- list current Account's Households and members;
- invite, accept, revoke/resend, add loginless Membership if approved, link
  Account, rename, leave/remove, change role, and transfer ownership;
- enforce Account -> Membership -> Household resource authorization;
- preserve at least one Owner under concurrent mutations;
- atomically consume one valid invitation and add Membership;
- instrument invitation and first-shared-action activation funnel;
- perform a provider-agnostic calendar-integration design spike only.

### Exit gate

The IDOR/BOLA matrix, invitation replay/expiry/rollback, and simultaneous
last-Owner operations pass against PostgreSQL. Cross-Household access is denied
without leaking unnecessary resource existence.

## Phase 4 — Tasks, routines, and reminder semantics

Goal: provide reliable household responsibility coordination.

### Delivery

- create, update, assign by Membership, complete, reopen, and archive tasks;
- add deliberately limited recurrence with IANA timezone, DST, missed
  occurrence, edit-one/edit-series, and idempotent occurrence semantics;
- preserve completion attribution/history by Membership identity;
- define notification recipient, preference, quiet-hours, deduplication, and
  status semantics;
- add a small worker/delivery table only if a beta reminder must fire without a
  request. Do not add a broker.

### Exit gate

Task authorization, concurrent transitions, recurrence/DST, retry, and
occurrence uniqueness pass. If reminder semantics are incomplete, ship
one-time tasks without notification infrastructure.

## Phase 5 — Shopping, Today, and optional Events

Goal: complete the smallest useful daily coordination loop.

### Delivery order

1. shared ShoppingList/ShoppingItem commands with stable item identity;
2. Today as an authorized, side-effect-free read composition;
3. internal HouseholdEvent only if capacity remains.

Start with one active shopping list without hard-coding that as a permanent
product limit. Define same-item conflict behavior and use refetch/polling before
realtime. Internal Events remain provider-neutral.

### Exit gate

Concurrent list behavior, authorized Today reads, and zero-write GET behavior
pass. If schedule pressure remains, omit Events before weakening Shopping,
Today, security, or correctness.

## Phase 6 — Beta trust and operations

Goal: make the product reproducibly operable and honestly beta-ready.

### Delivery

- immutable API container, isolated staging/production configuration, and
  explicit migrations;
- least-privilege identities/secrets, shared Data Protection keys, and rotation;
- structured logs, trace IDs, metrics, alerts, and runbooks;
- backup, restore, rollback, and deployment observation proof;
- rate limiting, dependency/vulnerability gates, and security review;
- machine-readable export, deletion, and shared-record-fate behavior;
- load/concurrency tests at measured risks;
- React Native/Expo client only against stable secured contracts.

### Exit gate

One artifact is built once and promoted; migration/rollback and backup/restore
are exercised; security and privacy gates pass; operational ownership exists;
the beta claim states remaining limitations explicitly.

## Decision and kill gates

| Gate | Required evidence | If it fails |
|---|---|---|
| Membership semantics | explicit ADR 0006 disposition and matching model | block durable membership schema |
| First vertical slice | green build/tests, trusted actor, PostgreSQL create/read | do not start Identity expansion |
| Collaboration | invite/lifecycle/authz/concurrency suite | do not expose Tasks/Shopping |
| Recurrence | deterministic timezone/DST/edit-series/idempotency tests | ship one-time tasks only |
| Notifications | concrete delivery semantics and retry need | do not add worker/push platform |
| Shopping concurrency | explicit same/different-item behavior | do not add realtime transport |
| Calendar integration | repeated need, provider priority, mapping/conflict model | keep implementation deferred |
| Public beta trust | authz, export/deletion, backup/restore, incident controls | no public beta claim |

## Portfolio evidence by phase

- Phase 1: one complete explicit vertical slice and real PostgreSQL proof.
- Phase 2: trusted identity separated from Household authority.
- Phase 3: resource authorization and concurrency-safe collaboration.
- Phase 4: time/recurrence modelling with honest infrastructure triggers.
- Phase 5: write aggregates separated from read composition.
- Phase 6: deployability, recovery, observability, and security evidence.

## Historical planning detail

The larger 2026-08-27 audit/masterplan is retained as a
[non-authoritative planning baseline](../../research/archive/planning-baseline-2026-08-27/HOMEPLATFORM-6-MONTH-MASTERPLAN.md).
It preserves detailed file/test/deployment reasoning but must not override this
roadmap or [NEXT-STEPS.md](NEXT-STEPS.md).
