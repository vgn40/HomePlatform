# HomePlatform Technical-Debt Register

> Ownership update — 2026-09-23 (local implementation, not a commit or deployment):
> a Household supports any number of Owners and must retain at least one while
> it exists. An Owner can add Member, Guest or Owner, including a Person without
> an ApplicationUser. Member/Guest and Owners with another Owner remaining may
> leave; only the last Owner receives 409 and must transfer or explicitly close.
> Transfer rejects an already-Owner target and changes only caller and target;
> its existing Application requirement for an Account-linked target is unchanged.
> Any Owner may close. These rules supersede older ownership statements below;
> concurrency protection remains open.

Status: **Authoritative debt register**  
Last reviewed: **2026-09-20**, committed `main@7162c35`

The affected Household slice passed 117 tests locally on 2026-09-20;
[NEXT-STEPS](NEXT-STEPS.md#verified-locally) records its scope. Historical closure
sections retain their original dates and counts. Uncommitted DeleteAccount
work is excluded from the main baseline. [NEXT-STEPS](NEXT-STEPS.md) alone
owns execution order.

Debt is a conscious correctness or maintainability obligation with a trigger
and exit gate. Unbuilt product scope is not automatically technical debt.

## Deliberate deferral — 2026-09-20

Product/domain development takes priority; accepted Person/Relationship targets
are not debt. The former hardening-first roadmap is replaced by incremental
Person work in NEXT-STEPS. Household authorization is committed in `7162c35`.
Existing uncommitted DeleteAccount source/tests are preserved and unchanged;
DeleteAccount completion is not claimed.

P1 below is an affected feature/release obligation, not an instruction to finish
all hardening before another product slice. Trigger work for concrete risk,
production relevance or feature dependency. New slices still require correct
invariants and resource authorization.

## Priority definitions

- **P0:** blocks compilation, a trusted contract, or the next durable schema.
- **P1:** resolve before the affected phase or public contract exits its gate.
- **P2:** acceptable now; revisit only at the stated trigger.
- **Deferred:** absence is deliberate, not debt.

## Active debt

| ID | Priority | Current evidence | Required action | Exit evidence |
|---|---|---|---|---|
| TD-005 | P1 | MembershipId, nullable AccountId, loginless membership, aggregate/database scoped uniqueness, and PostgreSQL round-trip are proven; verified link/unlink is absent; transfer/leave/close primitives exist | implement only explicit authorized link/unlink/lifecycle use cases when their roadmap gate is reached | Domain + PostgreSQL identity/cardinality/concurrency tests pass |
| TD-010 | P1 | Testing-only CreateHousehold has both test-auth and production-bearer coverage and Production is 404; Identity persistence, registration, sign-in, and Refresh exist, including renewal, expiry/security-stamp rejection, and real-token AccountId persistence tests; Account-reference integrity is verified locally; confirmation/recovery/revocation, protected-request current-Account validation, broader Membership authorization policy and real-bearer IDOR coverage remain incomplete; transfer/leave/close already enforce membership/Owner checks | complete applicable lifecycle, authorization and IDOR release gates when exposure warrants; immediate current-Account validation follows TD-020, not a universal Person prerequisite | security roadmap phase gates pass |
| TD-011 | Deferred production hardening | last-Owner, Account linking, invitation consumption, and future same-resource edits lack race proof | when simultaneous mutations or production guarantees require them, select database constraints/version/conditional operations and deterministic race tests | real PostgreSQL proves one winner/valid invariant |
| TD-013 | P1 | no production container/deploy/observability/backup/restore/export/deletion proof | review existing DeleteAccount work and complete applicable operational/privacy/security gates before real-user release | release gate is evidenced, not asserted |
| TD-014 | Deferred until recurrence is selected | recurrence/time behavior is undefined | decide IANA zone, DST, missed/edit-series and idempotency before persisting recurrence | deterministic and PostgreSQL tests for the chosen slice pass |
| TD-020 | Deferred security hardening | bearer access validation/HttpCurrentAccount do not reload Account existence; an issued short-lived access token may remain usable until expiry after deletion; Refresh validates separately | introduce immediate validation/revocation when a concrete product/release risk requires immediate cutoff; decide lifetime/acceptable window before release; retain current membership authorization | prove unexpired-token rejection if immediate cutoff is selected; separately prove Refresh rejection for deleted/invalid Accounts and removed membership denial |
| TD-021 | Deferred completion; affected release gate | DeleteAccount is absent from committed main; uncommitted endpoint/handler/adapter and transaction/rollback/tests exist, not verified complete | review existing work rather than duplicate it; resolve all memberships/ownership, atomicity, rollback, reauthentication, Person-transition compatibility and revocation policy before exposure | multi-Household deletion without loginless conversion, ownership refusal/resolution, rollback/race and shared/loginless-person handling; reject deleted-Account Refresh; document access expiry window or prove selected immediate cutoff |
| TD-022 | Deferred production hardening | LeaveHousehold, TransferOwnership and CloseHousehold exist through Testing-only HTTP/Application/Domain/persistence; Household concurrency token and race-safe ownership proof are absent | trigger concurrent ownership protection on simultaneous mutation/production needs; complete required authorization evidence for affected slices; record that leave rejects every Owner and close physically deletes current Household/memberships; decide future feature data behavior | Domain and PostgreSQL prove explicit transfer/closure, no loginless Owner, no ownerless continuation and concurrency/rollback safety |
| TD-023 | Deferred; concrete read-heavy feature | current repository loads tracked aggregates with Include; no paginated projection or measured query-plan evidence | introduce authorized DTO projections/AsNoTracking, bounded stable pagination and appropriate indexes with the selected read flow; inspect generated SQL, round trips, row counts and PostgreSQL plans against representative sizes | useful bounded reads without data leakage; reproducible measurements with data size/method; local synthetic data is not production evidence |
| TD-024 | Deferred hardening / release relevance | ProblemDetails/exception middleware and framework logs exist; error code/trace consistency and deeper observability remain incomplete | strengthen expected failure mapping/OpenAPI, safe 500s, correlation, diagnostics and redaction with real slices; decide sink/retention at release | useful diagnostics without secrets/personal payloads and consistent tested HTTP contracts |
| TD-025 | Deferred consolidation | scoped handlers/adapters/repository/DbContext, tests and architecture notes exist; no consolidated lifetime/test/trade-off review claimed | review disposal/captive dependencies when lifetimes change; explain actual Domain/Application/real-bearer/PostgreSQL coverage and HTTP-to-SQL transaction/validation boundaries as features evolve | focused risk evidence, honest test provenance and explainable source decisions; not an exhaustive artificial pre-feature matrix |

TD-019–022 record concrete integrity/security obligations under the
[adopted deletion design](../../privacy/DELETION-DESIGN.md), not a claim that
every unbuilt product feature is debt. They refine TD-005/010/011/013 and do not
close them. Their implementation order is [NEXT-STEPS.md](NEXT-STEPS.md).
Priority P1 requires resolution before the affected lifecycle/public contract;
no current build failure is claimed. TD-019 is closed below; TD-020–022 remain
open. The original decisions used `main@07eeb12` source inspection. Current
Account-reference local test evidence is historical: the 2026-09-14 review at
`main@bc03a00` plus then-uncommitted implementation/tests. Current source
inspection confirms the FK and lifecycle commits through `1ca1dd0`. TD-022 stays
open for concurrency and complete authorization proof, not missing primitives.

### Findings from the 2026-09-06 audit

| ID | Priority | Current evidence | Required action | Exit evidence |
|---|---|---|---|---|
| TD-018 | P1 | two dependency tests blacklist only outer HomePlatform assembly names | also guard forbidden frameworks/packages, direct project metadata, and Infrastructure-to-API direction | tests reject representative forbidden references without depending on emitted assembly use |

See the [audit](../DDD-ARCHITECTURE-AUDIT.md) for severity, exact locations,
verification limits, and lower-priority follow-ups. TD-018 remains open;
the original documentation audit did not change production or test source.

## Closed with Account-reference integrity — 2026-09-14

- **TD-019 — IMPLEMENTED / VERIFIED locally:** optional Account FK and lookup
  index exist in mapping, migration, designer and snapshot. EF `ClientNoAction`
  and PostgreSQL guarding deletion preserve linked memberships. The 12 existing
  integrity cases prove valid/null references, invalid-reference rejection,
  tracked/untracked deletion guards for Owner/Member/Guest, unreferenced Account
  deletion and optional model metadata. Two new upgrade cases prove valid data
  survives and dangling data fails without silent cleanup or invented Accounts.
  Build: zero warnings/errors; full solution: **119/119** (Domain 25,
  Application 16, Integration 78; **0 skipped**).
  [NEXT-STEPS](NEXT-STEPS.md#account-reference-integrity-verification--2026-09-14)
  links the permanent tests. Actual deployed rows/migration state are NOT VERIFIED;
  inspect existing data before environment upgrades. This does not close
  ownership lifecycle, protected-request validation or DeleteAccount.

## Closed with registration in e2fca98 — 2026-09-07

- **TD-016:** required/format/bounded input validation and recognized UserNameIndex
  duplicate-race translation are implemented. HTTP/PostgreSQL tests prove one
  201, one 400 EmailAlreadyExists, and one row; unrelated constraints remain
  safe 500. The independent NormalizedEmail lifecycle policy remains a security
  roadmap gate; no migration or Identity option change is claimed.
- **TD-017:** typed Application errors, ProblemDetails, RegisterAccountResponse,
  and generated OpenAPI 201/400 contracts are implemented and covered by tests.
  Solution baseline: Domain 25, Application 11, Integration 36; 72/72 passed.

## Closed with 2026-09-06 source evidence

- **TD-006:** Domain User was removed; ApplicationUser/Identity in Infrastructure
  is the only credential store. No Account aggregate is justified today.

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
- automatic cross-household Person matching, general ACL engine, external calendar sync, AI,
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
  Current hosted execution remains NOT VERIFIED by this audit.
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
