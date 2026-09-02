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
| TD-004 | P1 | bounded Name and initial Owner are proven, but `Members` returns the backing `List` as `IReadOnlyCollection` and can be downcast; one timestamp test uses `Thread.Sleep` | expose a non-downcastable read-only view and make the timestamp test deterministic without a speculative clock framework | focused Domain tests plus full suite and format gate pass |
| TD-005 | P1 | MembershipId, nullable AccountId, loginless membership, aggregate/database scoped uniqueness, and PostgreSQL round-trip are proven; link/unlink and lifecycle behavior do not exist | implement only explicit authorized link/unlink/lifecycle use cases when their roadmap gate is reached | Domain + PostgreSQL identity/cardinality/concurrency tests pass |
| TD-006 | P1 | Domain `User` overlaps future ASP.NET Core Identity ownership | remove/rename/justify it; never duplicate credentials | Phase 2 model and migration have one credential authority |
| TD-007 | P1 | hand-written handler tests now prove success, invalid input zero-write, cancellation forwarding, and repository-failure propagation; expected validation/unauthenticated conditions are still exception-shaped and no failing-current-account Application test exists | define stable expected Application outcomes and prove a failing current-account port causes zero writes | focused Application tests prove every expected outcome without EF or HTTP |
| TD-009 | P1 | two migrations and a local dotnet-ef 10.0.4 pin exist; fresh Testcontainers migration and local no-pending-model check pass | reproduce migration/model checks in discoverable green CI | clean CI migration plus no-pending-model result |
| TD-010 | P1 | a fake-authenticated Testing-only CreateHousehold endpoint exists and Production is 404; production Identity, Membership resource authorization, and IDOR coverage remain absent | add real Identity in Phase 2, then Account-to-Membership authorization and IDOR proof in Phase 3 | security roadmap phase gates pass |
| TD-011 | P1 | last-Owner, Account linking, invitation consumption, and future same-resource edits lack race proof | add database constraints/version/conditional operations and barrier tests | real PostgreSQL proves one winner/valid invariant |
| TD-012 | P1 | intended workflow content exists under non-discoverable `github/workflows`; no hosted run is evidenced; local format verification fails on whitespace, charset, and final-newline findings | move the reviewed workflow to `.github/workflows`, repair formatting narrowly, and run it in a clean environment | clean CI reproduces restore/build/test/migration/model/format evidence |
| TD-013 | P1 | no production container/deploy/observability/backup/restore/export/deletion proof | complete Phase 6 operational and trust work | public-beta gate is evidenced, not asserted |
| TD-014 | P1 | recurrence/time behavior is undefined | decide IANA zone, DST, missed/edit-series, idempotent occurrence rules | Phase 4 deterministic + PostgreSQL tests pass |
| TD-015 | P1 | OpenAPI is exposed only in Development while `POST /api/households` is mapped only in Testing, so no HTTP-exposed document contains the route and no document-generation test proves its declared 201/400/401 responses | generate and integration-test the Testing-host OpenAPI document without exposing fake-auth product routes in Production | an automated test finds the route and 201/400/401 responses in generated OpenAPI; Production remains 404 |

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
  and 50/50 tests pass.
- **TD-002:** ADR 0006 was intentionally accepted on 2026-08-30, and the
  implemented core identity model aligns with it.
- **TD-003:** `CreateHouseholdCommand` carries Name only; `ICurrentAccount`
  supplies the server-derived actor; anonymous/malformed/forged actor tests
  prove no impersonation and zero rows on rejection.
- **TD-008:** active EF mappings and repository registration exist;
  PostgreSQL save/clear/reload and the one-aggregate write path are proven.

These closures do not close the narrower follow-up gaps recorded in TD-004,
TD-005, TD-007, TD-009, TD-010, TD-011, TD-012, or TD-015.

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
