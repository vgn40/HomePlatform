# HomePlatform Documentation

This is the entry point for developer, architecture, product, and portfolio
documentation. Current/target authority is kept separate from historical plans
and generated research evidence.

## Start here

1. [Target architecture](architecture/target/TARGET-ARCHITECTURE.md) — what is
   implemented now and what the technical target is.
2. [Six-month roadmap](architecture/roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md)
   — the strategic delivery sequence.
3. [Next steps](architecture/roadmap/NEXT-STEPS.md) — the current executable
   order and stop-gate.

Current implementation remains early but includes one complete Testing-host
runtime path: CreateHousehold persists an authenticated actor's initial Owner
Membership to PostgreSQL. ADR 0006 is Accepted and the first migrations exist.
Production Identity/resource authorization, Phase 1 quality/CI gates, and the
frontend remain incomplete.

## Authoritative documents

| Topic | Authority |
|---|---|
| Architecture target | [TARGET-ARCHITECTURE.md](architecture/target/TARGET-ARCHITECTURE.md) |
| Bounded contexts | [CONTEXT-MAP.md](architecture/target/CONTEXT-MAP.md) |
| Domain model and language | [DOMAIN-MODEL.md](architecture/target/DOMAIN-MODEL.md) |
| Architecture decisions | [ADR index](architecture/adr/README.md) |
| Strategic roadmap | [HOMEPLATFORM-6-MONTH-MASTERPLAN.md](architecture/roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md) |
| Immediate implementation order | [NEXT-STEPS.md](architecture/roadmap/NEXT-STEPS.md) |
| Security plan | [SECURITY-ROADMAP.md](architecture/roadmap/SECURITY-ROADMAP.md) |
| Technical debt | [TECHNICAL-DEBT-REGISTER.md](architecture/roadmap/TECHNICAL-DEBT-REGISTER.md) |
| Product scope | [PRODUCT-SCOPE.md](product/PRODUCT-SCOPE.md) |

## Architecture

- [Architecture overview](architecture/README.md)
- [Architecture decision records](architecture/adr/README.md)
- [Target architecture](architecture/target/TARGET-ARCHITECTURE.md)
- [Context map](architecture/target/CONTEXT-MAP.md)
- [Domain model](architecture/target/DOMAIN-MODEL.md)

## Roadmap and security

- [Six-month masterplan](architecture/roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md)
- [Next development steps](architecture/roadmap/NEXT-STEPS.md)
- [Security roadmap](architecture/roadmap/SECURITY-ROADMAP.md)
- [Technical-debt register](architecture/roadmap/TECHNICAL-DEBT-REGISTER.md)

## Product

- [Product documentation index](product/README.md)
- [Product scope](product/PRODUCT-SCOPE.md)
- [Competitor summary](product/COMPETITOR-SUMMARY.md)

## Decision-relevant research

- [Competitor impact on DDD](architecture/research/COMPETITOR-IMPACT-ON-DDD.md)
- [Aggregate pressure test](architecture/research/AGGREGATE-PRESSURE-TEST.md)

These documents explain evidence and reasoning. They do not override ADRs or
the authoritative target/roadmap.

## Supporting evidence and history

Raw matrices, the validation notebook, detailed competitor profiles, source
registries, the full audit, and older planning baselines are under the
[research archive](research/archive/README.md). Archive files are retained for
traceability and are explicitly non-authoritative.

## Maintenance

- [Documentation map](DOCUMENTATION-MAP.md) records the 2026-08-29 cleanup.
- Relative links must remain valid after every move.
- Developer-facing docs use repository-relative paths, never a local home path.
- Historical ADRs are superseded with a later ADR, not rewritten.
- Refresh current-state claims when implementation moves; do not rewrite
  point-in-time archive evidence as current fact.
