# HomePlatform Technical-Debt Register

Status: **Authoritative debt register**  
Last reviewed: **2026-09-14**

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
| TD-010 | P1 | Testing-only CreateHousehold has both test-auth and production-bearer coverage and Production is 404; Identity persistence, registration, sign-in, and Refresh exist, including renewal, expiry/security-stamp rejection, and real-token AccountId persistence tests; Account-reference integrity is verified locally; confirmation/recovery/revocation, protected-request current-Account validation, Membership authorization, and IDOR coverage remain incomplete | follow NEXT-STEPS for TransferOwnership test-first, required ownership lifecycle/current-Account checks and DeleteAccount; complete wider lifecycle, authorization and IDOR release gates | security roadmap phase gates pass |
| TD-011 | P1 | last-Owner, Account linking, invitation consumption, and future same-resource edits lack race proof | add database constraints/version/conditional operations and barrier tests | real PostgreSQL proves one winner/valid invariant |
| TD-013 | P1 | no production container/deploy/observability/backup/restore/export/deletion proof | follow the brought-forward deletion sequence and complete operational/privacy/security gates before real-user release | release gate is evidenced, not asserted |
| TD-014 | P1 | recurrence/time behavior is undefined | decide IANA zone, DST, missed/edit-series, idempotent occurrence rules | Phase 4 deterministic + PostgreSQL tests pass |
| TD-020 | P1 | bearer access validation and HttpCurrentAccount do not verify current Account existence; Refresh security-stamp validation is a separate check | add current-Account existence/validity for protected product requests and current membership for Household authorization before DeleteAccount | an unexpired token from a deleted Account cannot authorize new product requests; removed membership loses Household access |
| TD-021 | P1 | DeleteAccount is absent; adopted multi-Household deletion and immediate access termination are not enforced | implement after TD-019/020/022; resolve ownership, explicitly delete all Account-linked memberships across all Households without loginless conversion, refuse unresolved last Owner, atomically apply approved lifecycle and delete Identity Account | multi-Household membership deletion/no-conversion, shared/loginless-person, refusal, rollback/race, feature-specific reference cleanup and post-commit access/refresh denial tests |
| TD-022 | P1 | LeaveHousehold, TransferOwnership and CloseHousehold are absent; Owner-null constructor guard is not lifecycle/concurrency proof | next CODE feature: TransferOwnership test-first, then explicit authorized leave/close primitives; decide standalone leave/close and feature-specific data behavior; DeleteAccount membership deletion is already ADOPTED; never auto-promote; preserve Account-linked Owner for continuing Household | Domain and PostgreSQL prove explicit transfer/closure, no loginless Owner, no ownerless continuation and concurrency/rollback safety |

TD-019–022 record concrete integrity/security obligations under the
[adopted deletion design](../../privacy/DELETION-DESIGN.md), not a claim that
every unbuilt product feature is debt. They refine TD-005/010/011/013 and do not
close them. Their implementation order is [NEXT-STEPS.md](NEXT-STEPS.md).
Priority P1 requires resolution before the affected lifecycle/public contract;
no current build failure is claimed. TD-019 is closed below; TD-020–022 remain
open. The original decisions used `main@07eeb12` source inspection. Current
Account-reference evidence is the 2026-09-14 local test review at `main@bc03a00`
plus the uncommitted implementation/tests.

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
