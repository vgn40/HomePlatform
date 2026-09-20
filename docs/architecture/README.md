# HomePlatform Architecture

This section explains the implemented architecture, the approved direction,
and the decisions and gates that still constrain implementation. It is intentionally
separate from product research and generated evidence.

HomePlatform models shared life across households, relationships and changing
family structures. Read the [product direction](../product/PRODUCT-SCOPE.md)
before the detailed engineering requirements.

## Current state — AS-IS

Committed `main@7162c35`, reviewed 2026-09-20:

- Four layer assemblies retain the intended dependency direction.
- Household is the small product-domain aggregate; Identity is an
  Infrastructure-owned supporting capability.
- Registration, bearer sign-in and Refresh are mapped in all environments.
- CreateHousehold, TransferOwnership, LeaveHousehold and CloseHousehold have
  complete source paths through HTTP, Application, Domain and persistence;
  all Household endpoints remain Testing-only.
- Stable Membership identity, nullable Account FK, scoped link uniqueness and
  operation-specific membership/Owner checks exist; nonmember concealment
  returns 404, while known non-Owner members receive 403 for transfer/close.
- Current-Account validation, optimistic concurrency, read projections and
  broader release gates remain incomplete. DeleteAccount work is uncommitted.
- The affected Domain/Application/PostgreSQL integration slice passed 117 tests
  locally; hosted CI/deployment remain unverified.

See [NEXT-STEPS](roadmap/NEXT-STEPS.md) for verification scope. The
[2026-09-06 audit](DDD-ARCHITECTURE-AUDIT.md) is historical evidence.

## Target state

[ADR 0007](adr/0007-person-as-stable-human-identity.md) accepts Person as stable
human identity, separate from Account credentials and HouseholdMembership.
Relationship connects Persons independently of co-residence; multiple Household
memberships do not create multiple Persons. Child/parent/grandparent are
contextual relationship roles, not entity subtypes. Person and Relationship
are not implemented. CareCircle/CareCircleMembership remain **FUTURE**.

Keep one deployable modular monolith. Domain owns business invariants;
Application owns explicit use cases and ports; Infrastructure owns EF Core,
PostgreSQL, Identity, and other adapters; API owns HTTP and composition.
Business modules remain folder/namespace boundaries until measured pressure
justifies stronger isolation.

Read:

1. [Target architecture](target/TARGET-ARCHITECTURE.md)
2. [Context map](target/CONTEXT-MAP.md)
3. [Domain model](target/DOMAIN-MODEL.md)
4. [Architecture decision records](adr/README.md)

## Roadmap and gates

- [Six-month plan](roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md) is historical;
  its feature schedule has been superseded.
- [Next steps](roadmap/NEXT-STEPS.md) is the sole executable order: incremental
  Person design and product flows now; hardening when risk/dependencies justify it.
- [Security roadmap](roadmap/SECURITY-ROADMAP.md) is the authoritative security
  plan.
- [Technical-debt register](roadmap/TECHNICAL-DEBT-REGISTER.md) is the
  authoritative debt list.

## Adopted Account and Household lifecycle

[DELETION-DESIGN.md](../privacy/DELETION-DESIGN.md) owns the current deletion and
ownership decisions. Account and Household lifecycles remain separate; the last
Owner must explicitly transfer or close, and no Membership is automatically
promoted. Owner always requires a real Account. The guarding nullable AccountId
FK and transfer/leave/close primitives now exist. Current-Account validation,
concurrency proof and committed DeleteAccount completion remain follow-ups.
Standalone leave currently refuses every Owner; close physically deletes the
Household and memberships while retaining Accounts. Future shared-feature data
policies and destination acceptance remain open in the canonical design.

## Decision-relevant research

- [Competitor impact on DDD](research/COMPETITOR-IMPACT-ON-DDD.md)
- [Aggregate pressure test](research/AGGREGATE-PRESSURE-TEST.md)

These are evidence and reasoning inputs, not accepted decisions. Raw research,
historical planning baselines, matrices, and notebooks live in the
[supporting-evidence archive](../research/archive/README.md).

## Architectural guardrails

- Domain depends on no framework or outer project.
- Application depends on Domain, not Infrastructure or API.
- Infrastructure implements Application ports and owns technical details.
- API is the composition root. Product endpoints use Application; the current
  DbContext-backed `/ready` connectivity probe is an operational exception.
- Actor identity comes from trusted server authentication, never request data.
- Household access is resource authorization through a current Membership.
- Use real PostgreSQL/Testcontainers for persistence and concurrency proof.
- Do not add MediatR, generic repositories, UnitOfWork wrappers, event sourcing,
  brokers, microservices, or speculative abstractions without a documented
  trigger.
