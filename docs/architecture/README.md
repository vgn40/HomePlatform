# HomePlatform Architecture

This section explains the implemented architecture, the approved direction,
and the decisions and gates that still constrain implementation. It is intentionally
separate from product research and generated evidence.

## Current state

- Four production layer assemblies with the intended project dependencies.
- One small Household aggregate and a PostgreSQL-backed, authenticated
  Testing-only CreateHousehold slice implement the core of accepted ADR 0006.
- Infrastructure owns roleless Identity persistence. Account Registration is
  implemented and committed in `e2fca98`, with an anonymous endpoint in every
  environment, including Production; the verified solution baseline is 72/72
  tests (Domain 25, Application 11, Integration 36).
- Sign-in/session authentication, Household resource authorization, deployment,
  and frontend remain incomplete. Registration does not complete those gates.
- Tactical DDD is present in Household; multiple implemented bounded contexts
  and strategic DDD are not established.

See the [2026-09-06 DDD/Clean Architecture audit](DDD-ARCHITECTURE-AUDIT.md)
for historical source evidence and the registration completion follow-up.

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
- API is the composition root. Product endpoints use Application; the current
  DbContext-backed `/ready` connectivity probe is an operational exception.
- Actor identity comes from trusted server authentication, never request data.
- Household access is resource authorization through a current Membership.
- Use real PostgreSQL/Testcontainers for persistence and concurrency proof.
- Do not add MediatR, generic repositories, UnitOfWork wrappers, event sourcing,
  brokers, microservices, or speculative abstractions without a documented
  trigger.
