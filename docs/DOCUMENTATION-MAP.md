# Documentation Map

Cleanup ID: `HOMEPLATFORM-DOCS-ARCHITECTURE-CLEANUP-01`  
Snapshot: `main@a87a817` with an existing dirty working tree  
Cleanup date: **2026-08-29**

This map records the old location, resulting location, primary classification,
and reason. No unique document was deleted. Historical plans and raw/generated
research were archived rather than presented as current authority.

## Existing repository and component documentation

| Old path | New path | Action | Reason |
|---|---|---|---|
| `README.md` | `README.md` | REWRITE | concise portfolio entry, exact current status, primary links, pinned migration gate |
| `backend/src/HomePlatform.Application/README.md` | same | REWRITE | replaces false empty-layer statement with incomplete current slice and actor rule |
| `backend/src/HomePlatform.Domain/README.md` | same | REWRITE | records current concepts and proposed Membership decision without claiming implementation |
| `backend/src/HomePlatform.Infrastructure/Persistence/Migrations/README.md` | same | REWRITE | records the ADR/mapping/tooling stop-gate before first migration |
| `frontend/README.md` | same | REWRITE | preserves placeholder status and links product scope |
| `docs/architecture/README.md` | same | REWRITE | becomes architecture index/summary rather than competing authority |

## Architecture decisions

| Old path | New path | Action | Reason |
|---|---|---|---|
| `docs/architecture/adr/0001-use-modular-monolith.md` | same | KEEP | accepted historical decision remains authoritative |
| `docs/architecture/adr/0002-use-csharp-dotnet-backend.md` | same | KEEP | accepted historical decision remains authoritative |
| `docs/architecture/adr/0003-keep-domain-framework-independent.md` | same | KEEP | accepted historical decision remains authoritative |
| `docs/architecture/adr/0004-use-postgresql.md` | same | KEEP | accepted historical decision remains authoritative |
| `docs/architecture/adr/0005-use-entity-framework-core.md` | same | KEEP | accepted historical decision remains authoritative |
| `docs/architecture/adr/0006-separate-account-and-household-membership-identity.md` | same | KEEP | concurrent proposed ADR is preserved as a review gate, not marked Accepted |
| — | `docs/architecture/adr/README.md` | CREATE | sequential index, status legend, and supersession discipline |

## Target architecture and roadmap

| Old path | New path | Action | Reason |
|---|---|---|---|
| `docs/architecture/masterplan/TARGET-ARCHITECTURE.md` | `docs/research/archive/planning-baseline-2026-08-27/TARGET-ARCHITECTURE.md` | ARCHIVE | preserves detailed 2026-08-27 target/audit assumptions as non-authoritative history |
| — | `docs/architecture/target/TARGET-ARCHITECTURE.md` | CREATE | one concise current-versus-target technical authority |
| `docs/architecture/research/PROPOSED-BOUNDED-CONTEXTS.md` | `docs/research/archive/competitor-audit-2026-08-29/PROPOSED-BOUNDED-CONTEXTS.md` | MERGE | unique proposal retained; current/proposed/deferred conclusions consolidated into Context Map |
| — | `docs/architecture/target/CONTEXT-MAP.md` | CREATE | one current bounded-context authority |
| — | `docs/architecture/target/DOMAIN-MODEL.md` | CREATE | one current/proposed DDD language and model authority |
| `docs/architecture/masterplan/HOMEPLATFORM-6-MONTH-MASTERPLAN.md` | `docs/research/archive/planning-baseline-2026-08-27/HOMEPLATFORM-6-MONTH-MASTERPLAN.md` | ARCHIVE | preserves 1,340-line audit/plan while removing stale current authority |
| — | `docs/architecture/roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md` | CREATE | one readable strategic roadmap incorporating research deltas |
| `docs/architecture/masterplan/NEXT-10-STEPS.md` | `docs/research/archive/planning-baseline-2026-08-27/NEXT-10-STEPS.md` | ARCHIVE | preserves detailed obsolete sequence as historical evidence |
| — | `docs/architecture/roadmap/NEXT-STEPS.md` | CREATE | one executable order with ADR 0006 and live source stop-gates |
| `docs/architecture/masterplan/SECURITY-ROADMAP.md` | `docs/architecture/roadmap/SECURITY-ROADMAP.md` | MOVE | single security authority moved to its conceptual home and terminology refreshed |
| `docs/architecture/masterplan/TECHNICAL-DEBT-REGISTER.md` | `docs/research/archive/planning-baseline-2026-08-27/TECHNICAL-DEBT-REGISTER.md` | ARCHIVE | preserves detailed dated register and stale diagnoses |
| — | `docs/architecture/roadmap/TECHNICAL-DEBT-REGISTER.md` | CREATE | one concise active debt authority |

## Decision-relevant architecture research

| Old path | New path | Action | Reason |
|---|---|---|---|
| `docs/architecture/research/COMPETITOR-IMPACT-ON-DDD.md` | same | KEEP | high-value evidence chain remains prominent but non-authoritative |
| `docs/architecture/research/AGGREGATE-PRESSURE-TEST.md` | same | KEEP | high-value DDD/aggregate reasoning remains prominent |
| `docs/architecture/research/ROADMAP-CHANGE-PROPOSAL.md` | `docs/research/archive/competitor-audit-2026-08-29/ROADMAP-CHANGE-PROPOSAL.md` | MERGE | changes integrated into current roadmap/next steps; proposal retained for traceability |

## Competitor audit and generated evidence

| Old path | New path | Action | Reason |
|---|---|---|---|
| `docs/research/competitors/HOMEPLATFORM-COMPETITOR-AUDIT.md` | `docs/research/archive/competitor-audit-2026-08-29/HOMEPLATFORM-COMPETITOR-AUDIT.md` | ARCHIVE | full 644-line report remains evidence, not primary navigation |
| `docs/research/competitors/COMPETITOR-PROFILES.md` | `docs/research/archive/competitor-audit-2026-08-29/COMPETITOR-PROFILES.md` | ARCHIVE | detailed time-bound profiles are supporting evidence |
| `docs/research/competitors/COMPETITOR-FEATURE-MATRIX.csv` | `docs/research/archive/competitor-audit-2026-08-29/COMPETITOR-FEATURE-MATRIX.csv` | ARCHIVE | generated 1,875-row matrix retained for traceability |
| `docs/research/competitors/USER-PAIN-MATRIX.csv` | `docs/research/archive/competitor-audit-2026-08-29/USER-PAIN-MATRIX.csv` | ARCHIVE | machine-readable evidence kept out of primary hierarchy |
| `docs/research/competitors/ARCHITECTURE-EVIDENCE.md` | `docs/research/archive/competitor-audit-2026-08-29/ARCHITECTURE-EVIDENCE.md` | ARCHIVE | point-in-time source/repository evidence retained |
| `docs/research/competitors/FEATURE-GAP-ANALYSIS.md` | `docs/research/archive/competitor-audit-2026-08-29/FEATURE-GAP-ANALYSIS.md` | MERGE | P0/P1 conclusions incorporated into product scope and roadmap; detail archived |
| `docs/research/competitors/PRODUCT-OPPORTUNITY-MAP.md` | `docs/research/archive/competitor-audit-2026-08-29/PRODUCT-OPPORTUNITY-MAP.md` | MERGE | positioning/experiments summarized; scored detail archived |
| `docs/research/competitors/SOURCES.md` | `docs/research/archive/competitor-audit-2026-08-29/SOURCES.md` | ARCHIVE | source/method registry remains provenance authority only |
| `docs/research/competitors/AUDIT-VALIDATION.ipynb` | `docs/research/archive/competitor-audit-2026-08-29/AUDIT-VALIDATION.ipynb` | ARCHIVE | reproducibility artifact retained, outputs cleared, archival assumptions disclosed |
| — | `docs/product/COMPETITOR-SUMMARY.md` | CREATE | concise decision-relevant product/architecture conclusions |

## New navigation and product documents

| Old path | New path | Action | Reason |
|---|---|---|---|
| — | `docs/README.md` | CREATE | primary documentation entry point |
| — | `docs/DOCUMENTATION-MAP.md` | CREATE | audit/action traceability |
| — | `docs/product/README.md` | CREATE | product navigation |
| — | `docs/product/PRODUCT-SCOPE.md` | CREATE | one authoritative beta scope |
| — | `docs/research/README.md` | CREATE | explains research versus authority |
| — | `docs/research/archive/README.md` | CREATE | labels all archive content as supporting evidence |
| — | `docs/research/archive/competitor-audit-2026-08-29/README.md` | CREATE | dated evidence inventory and links to primary conclusions |
| — | `docs/research/archive/planning-baseline-2026-08-27/README.md` | CREATE | historical planning boundary and current links |
| — | `.markdownlint-cli2.jsonc` | CREATE | lint primary docs with practical prose/table rules while treating archive snapshots as preserved evidence |

## Removal summary

No document or evidence artifact was deleted. Duplicate authority was removed
through rewrite, merge, and explicit archival status—not loss of unique
information.
