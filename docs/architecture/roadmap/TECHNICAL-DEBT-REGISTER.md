# HomePlatform Technical-Debt Register

Status: **Authoritative debt register**  
Last reviewed: **2026-08-29**

Debt is a conscious correctness or maintainability obligation with a trigger
and exit gate. Unbuilt product scope is not automatically technical debt.

## Priority definitions

- **P0:** blocks compilation, a trusted contract, or the next durable schema.
- **P1:** resolve before the affected phase or public contract exits its gate.
- **P2:** acceptable now; revisit only at the stated trigger.
- **Deferred:** absence is deliberate, not debt.

## Active debt

| ID | Priority | Current evidence | Required action | Exit evidence |
|---|---|---|---|---|
| TD-001 | P0 | Household/HouseholdMember now use new Account/Membership signatures, while handler/tests still use prior APIs; fresh build not verified | align approved Domain, handler, and tests after ADR gate | full solution build and every test project green |
| TD-002 | P0 | uncommitted Domain code prototypes ADR 0006 while the ADR remains Proposed | explicitly accept/reject/revise Account/Membership identity before EF mapping | ADR disposition and all target/roadmap/model/source semantics agree |
| TD-003 | P0 | current command carries `CreatorUserId`; no trusted actor port exists | command contains business input only; inject trusted Account context | forged/missing actor tests prove zero impersonation/writes |
| TD-004 | P1 | Household name/collection/owner behavior is incomplete or inconsistent | enforce bounded name, invariant-safe Owner, non-downcastable member view | deterministic Domain tests pass before mapping |
| TD-005 | P1 | HouseholdMember now generates MembershipId and permits AccountId, but lacks approved link/unlink/lifecycle and persistence semantics | finish only the ADR-approved model and link lifecycle | Domain + PostgreSQL identity/cardinality tests pass |
| TD-006 | P1 | Domain `User` overlaps future ASP.NET Core Identity ownership | remove/rename/justify it; never duplicate credentials | Phase 2 model and migration have one credential authority |
| TD-007 | P1 | CreateHousehold has no behavior tests and throws generic expected failures | add explicit outcomes and hand-written handler tests | zero-write invalid/unauthenticated cases and success proven |
| TD-008 | P1 | DbContext has no active Household model; mapping files are empty; repository is unregistered | complete focused mapping/repository after ADR | save/clear/reload and one-commit PostgreSQL proof |
| TD-009 | P1 | no migration; root previously suggested floating global EF tooling | pin local `dotnet-ef`, review first migration, prove from zero | CI migration plus no-pending-model result |
| TD-010 | P1 | no product endpoint, authentication, resource authorization, or IDOR suite | Testing-only first slice, then real Identity and Account-to-Membership authz | security roadmap phase gates pass |
| TD-011 | P1 | last-Owner, Account linking, invitation consumption, and future same-resource edits lack race proof | add database constraints/version/conditional operations and barrier tests | real PostgreSQL proves one winner/valid invariant |
| TD-012 | P1 | no CI; current format/build state can drift | add restore/audit/build/test/migration/format workflow | clean CI reproduces local evidence |
| TD-013 | P1 | no production container/deploy/observability/backup/restore/export/deletion proof | complete Phase 6 operational and trust work | public-beta gate is evidenced, not asserted |
| TD-014 | P1 | recurrence/time behavior is undefined | decide IANA zone, DST, missed/edit-series, idempotent occurrence rules | Phase 4 deterministic + PostgreSQL tests pass |

## Accepted shortcuts

| ID | Shortcut | Why acceptable now | Revisit trigger |
|---|---|---|---|
| AS-001 | raw Guid IDs | small model and simple boundaries | repeated wrong-ID defects justify stronger types |
| AS-002 | `Guid.NewGuid()` in Domain | no deterministic/external ID behavior | identity policy becomes business-relevant |
| AS-003 | `DateTime.UtcNow` for simple audit timestamps | little current time behavior | recurrence/deadline tests require controlled time |
| AS-004 | Owner/Member/Guest enum | small readable role set | repeated permissions exceed role/resource policy |
| AS-005 | small Result primitive | few Domain outcomes | several use cases need typed values/errors |
| AS-006 | one DbContext/database/schema | simplest monolith transaction/deployment | measured ownership or migration conflicts |
| AS-007 | four layer assemblies with feature folders | boundaries fit current team/product | recurring compile-time cross-module violations |
| AS-008 | direct handlers without MediatR | explicit and testable | repeated pipeline behavior proves net simplicity |
| AS-009 | focused repository commits one aggregate | clear first-slice transaction | real multi-aggregate use case needs a focused transaction port |
| AS-010 | no API version prefix | no incompatible released client | client versions must coexist |
| AS-011 | one active shopping list initially | minimum usable beta | validated need for multiple lists/stores |
| AS-012 | polling/refetch before realtime | no validated sub-second need | measured UX requires faster collaboration |

## Deliberately deferred

Do not introduce these as “cleanup” without their target-architecture trigger:

- BaseEntity/AggregateRoot frameworks;
- generic repository or generic UnitOfWork wrapper;
- MediatR/CQRS framework, AutoMapper, or speculative validation framework;
- Domain Events, outbox, broker, event sourcing, or scheduler platform;
- Redis, SignalR, microservices, per-module database, Kubernetes, or
  multi-region;
- global Person/Profile, general ACL engine, external calendar sync, AI,
  location, meals, expenses, maintenance, rewards, or full RRULE.

## Recently closed documentation debt

The 2026-08-29 documentation cleanup:

- replaced the false “no product concepts” README statements with exact
  current status;
- established one authority for architecture, context, domain model, roadmap,
  next steps, security, and debt;
- separated raw research/generated evidence from developer-facing docs;
- removed floating EF-tool guidance and local absolute paths from primary docs.

These closures do not prove application behavior.

## Review cadence

At each phase exit:

1. close items only with evidence;
2. add accepted shortcuts with a trigger;
3. promote shortcuts only when their trigger occurs;
4. reject abstractions without evidence;
5. refresh current-state wording without rewriting historical ADRs.

The detailed 2026-08-27 register is retained as a
[non-authoritative baseline](../../research/archive/planning-baseline-2026-08-27/TECHNICAL-DEBT-REGISTER.md).
