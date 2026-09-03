# HomePlatform Technical-Debt Register

Status: **Authoritative debt register**  
Last reviewed: **2026-09-02**

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
| TD-005 | P1 | MembershipId, nullable AccountId, loginless membership, aggregate/database scoped uniqueness, and PostgreSQL round-trip are proven; link/unlink and lifecycle behavior do not exist | implement only explicit authorized link/unlink/lifecycle use cases when their roadmap gate is reached | Domain + PostgreSQL identity/cardinality/concurrency tests pass |
| TD-006 | P1 | Domain `User` overlaps future ASP.NET Core Identity ownership | remove/rename/justify it; never duplicate credentials | Phase 2 model and migration have one credential authority |
| TD-010 | P1 | a fake-authenticated Testing-only CreateHousehold endpoint exists and Production is 404; production Identity, Membership resource authorization, and IDOR coverage remain absent | add real Identity in Phase 2, then Account-to-Membership authorization and IDOR proof in Phase 3 | security roadmap phase gates pass |
| TD-011 | P1 | last-Owner, Account linking, invitation consumption, and future same-resource edits lack race proof | add database constraints/version/conditional operations and barrier tests | real PostgreSQL proves one winner/valid invariant |
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

## Closed with 2026-09-02 evidence

- **TD-001:** Domain, handler, and all test projects are aligned; restore/build
  and 57/57 tests pass with 0 build warnings and 0 build errors.
- **TD-002:** ADR 0006 was intentionally accepted on 2026-08-30, and the
  implemented core identity model aligns with it.
- **TD-003:** `CreateHouseholdCommand` carries Name only; `ICurrentAccount`
  supplies the server-derived actor; anonymous/malformed/forged actor tests
  prove no impersonation and zero rows on rejection.
- **TD-004:** `Members` now exposes a non-downcastable live read-only view, and
  the Domain timestamp test is deterministic without a clock abstraction or
  `Thread.Sleep`. Repository-wide formatting was independently closed under
  TD-012 rather than being mixed into the Domain change.
- **TD-007:** focused Application tests prove explicit Success, Invalid, and
  Unauthenticated outcomes, normalized success data, trusted initial Owner,
  zero writes for invalid and unauthenticated inputs, cancellation forwarding,
  null-command behavior, and propagation of unexpected repository failures.
- **TD-008:** active EF mappings and repository registration exist;
  PostgreSQL proves save/clear/reload, exactly one asynchronous SaveChanges
  invocation, cancellation with zero rows, transaction rollback with zero
  partial rows, loginless Membership persistence, and scoped uniqueness between
  stale aggregate instances. Stable conflict mapping is deferred to the first
  membership mutation use case with a meaningful expected conflict; it is not
  an expected CreateHousehold outcome.
- **TD-009:** the repository-pinned dotnet-ef 10.0.4 applied both migrations to
  a fresh PostgreSQL 18.6 database, and the pending-model check passed locally;
  the same gates are present in the discoverable backend workflow.
- **TD-012:** the reviewed workflow now lives under `.github/workflows`; the
  historical indentation, trailing-whitespace, final-newline, and migration BOM
  findings were repaired mechanically; the full local CI equivalent is green.
  Hosted execution remains not verified until commit and push.
- **TD-015:** Testing exposes generated OpenAPI containing the Testing-only
  CreateHousehold operation with 201/400/401 and the response schema, while
  Production continues not to expose the product route.

These closures do not close the narrower follow-up gaps recorded in TD-005,
TD-010, or TD-011.

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
