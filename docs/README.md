# HomePlatform Documentation

This is the entry point for developer, architecture, product, and portfolio
documentation. Current/target authority is kept separate from historical plans
and generated research evidence.

## Start here

1. [Product scope](product/PRODUCT-SCOPE.md) — shared life across households,
   relationships and changing family structures.
2. [Architecture overview](architecture/README.md) — what exists today.
3. [Domain model](architecture/target/DOMAIN-MODEL.md) and
   [ADR 0007](architecture/adr/0007-person-as-stable-human-identity.md) — accepted
   Person-centered target, distinct from implementation and future care concepts.
4. [Next steps](architecture/roadmap/NEXT-STEPS.md) — sole execution order;
   design the smallest incremental Person foundation next.
5. [Technical debt](architecture/roadmap/TECHNICAL-DEBT-REGISTER.md),
   [security](architecture/roadmap/SECURITY-ROADMAP.md) and
   [deletion design](privacy/DELETION-DESIGN.md) — known risks and release gates.
6. [Future concepts](product/PRODUCT-SCOPE.md#future-care-and-cross-household-exploration)
   — CareCircle and intentionally undecided cross-Household ideas.

**AS-IS:** committed `main@7162c35` has registration, bearer sign-in, refresh,
Account-linked memberships, the Account FK and create/transfer/leave/close.
Household routes are Testing-only; nonmember concealment is committed and the
affected 117-test slice passed locally. DeleteAccount remains uncommitted,
unchanged and not claimed complete. **TARGET:** Person and
Relationship are accepted but absent from source. **FUTURE:** CareCircle is
planned, not implemented. See [local verification](architecture/roadmap/NEXT-STEPS.md#verified-locally);
no hosted CI or deployment verification is claimed.

## Authoritative documents

| Topic | Authority |
|---|---|
| Architecture target | [TARGET-ARCHITECTURE.md](architecture/target/TARGET-ARCHITECTURE.md) |
| Candidate context boundaries | [CONTEXT-MAP.md](architecture/target/CONTEXT-MAP.md) |
| Domain model and language | [DOMAIN-MODEL.md](architecture/target/DOMAIN-MODEL.md) |
| Architecture decisions | [ADR index](architecture/adr/README.md) |
| Historical feature plan (non-authoritative) | [HOMEPLATFORM-6-MONTH-MASTERPLAN.md](architecture/roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md) |
| Immediate implementation order | [NEXT-STEPS.md](architecture/roadmap/NEXT-STEPS.md) |
| Security plan | [SECURITY-ROADMAP.md](architecture/roadmap/SECURITY-ROADMAP.md) |
| Technical debt | [TECHNICAL-DEBT-REGISTER.md](architecture/roadmap/TECHNICAL-DEBT-REGISTER.md) |
| Product scope | [PRODUCT-SCOPE.md](product/PRODUCT-SCOPE.md) |
| Account deletion and Household lifecycle | [DELETION-DESIGN.md](privacy/DELETION-DESIGN.md) |

## Architecture

- [Architecture overview](architecture/README.md)
- [DDD/Clean Architecture audit — 2026-09-06](architecture/DDD-ARCHITECTURE-AUDIT.md)
- [Architecture decision records](architecture/adr/README.md)
- [Target architecture](architecture/target/TARGET-ARCHITECTURE.md)
- [Context map](architecture/target/CONTEXT-MAP.md)
- [Domain model](architecture/target/DOMAIN-MODEL.md)

## Roadmap and security

- [Historical six-month masterplan](architecture/roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md)
- [Next development steps](architecture/roadmap/NEXT-STEPS.md)
- [Security roadmap](architecture/roadmap/SECURITY-ROADMAP.md)
- [Technical-debt register](architecture/roadmap/TECHNICAL-DEBT-REGISTER.md)

## Privacy and lifecycle

- [Deletion design](privacy/DELETION-DESIGN.md) — adopted Account/Household
  lifecycle, ownership rules, open decisions, and implementation dependencies.
- [Data inventory](privacy/DATA-INVENTORY.md) — current repository evidence.
- [Retention policy](privacy/RETENTION-POLICY.md) — open periods and proposed preparation.
- [Processing register](privacy/PROCESSING-REGISTER.md) — purposes and unresolved legal/operational facts.
- [Privacy notice requirements](privacy/PRIVACY-NOTICE-REQUIREMENTS.md) — initial publication requirements.

The lifecycle decisions are ADOPTED. Account-reference integrity and the three
Household lifecycle operations are IMPLEMENTED in the committed snapshot;
current-Account validity and DeleteAccount remain incomplete. Historical local
test evidence is explicitly dated in [NEXT-STEPS](architecture/roadmap/NEXT-STEPS.md#verified-locally).

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
- Preserve historical ADR context; dated current-decision addenda may clarify
  follow-up decisions. Supersede a replaced decision explicitly.
- Refresh current-state claims when implementation moves; do not rewrite
  point-in-time archive evidence as current fact.
