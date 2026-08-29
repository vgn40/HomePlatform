# HomePlatform Architecture

This section explains the implemented architecture, the approved direction,
and the proposed decisions that still block implementation. It is intentionally
separate from product research and generated evidence.

## Current state

- One ASP.NET Core modular-monolith foundation with Domain, Application,
  Infrastructure, and API projects.
- Health/readiness and PostgreSQL connectivity exist.
- Household, HouseholdMember, roles, and an incomplete CreateHousehold slice
  exist in the current working tree.
- The constructor, handler, and tests are not aligned in the inspected
  2026-08-29 snapshot; a current green solution has not been established by
  this documentation task.
- No product endpoint, authentication/authorization implementation, active EF
  mapping, migration, end-to-end household flow, or frontend exists.

## Target state

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

- [Six-month roadmap](roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md) is the
  authoritative strategic sequence.
- [Next steps](roadmap/NEXT-STEPS.md) is the authoritative executable order.
- [Security roadmap](roadmap/SECURITY-ROADMAP.md) is the authoritative security
  plan.
- [Technical-debt register](roadmap/TECHNICAL-DEBT-REGISTER.md) is the
  authoritative debt list.

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
- API is the composition root and never becomes the data-access layer.
- Actor identity comes from trusted server authentication, never request data.
- Household access is resource authorization through a current Membership.
- Use real PostgreSQL/Testcontainers for persistence and concurrency proof.
- Do not add MediatR, generic repositories, UnitOfWork wrappers, event sourcing,
  brokers, microservices, or speculative abstractions without a documented
  trigger.
