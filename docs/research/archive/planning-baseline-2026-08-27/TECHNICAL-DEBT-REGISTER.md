# HomePlatform technical-debt register

> **ARCHIVED — NON-AUTHORITATIVE:** Point-in-time register from 2026-08-27.
> Use the current
> [technical-debt register](../../../architecture/roadmap/TECHNICAL-DEBT-REGISTER.md).
> Several current-state entries below have been superseded.

Audit date: 2026-08-27  
Repository snapshot: `main` at `a87a8176676a118a2a684f02d0a2ab7f74eef182` with existing dirty worktree  
Policy: debt is a conscious tradeoff with a trigger/date, not a synonym for unfinished product scope.

## Priority definitions

- **P0:** blocks compilation, correctness, or a safe next step; resolve before new work.
- **P1:** resolve in the named near-term phase before the affected contract becomes durable/public.
- **P2:** acceptable for now with a concrete future trigger.
- **Deferred:** do not spend time on it until evidence satisfies the decision gate.

## Current problems to fix soon

| ID | Priority | Evidence/current impact | Required action | Due/exit evidence |
|---|---|---|---|---|
| TD-001 | P0 | dirty `HouseholdRole` contains Owner/Member/Guest and no namespace; Domain tests still use Parent/Child, causing six CS0117 errors | settle role vocabulary/storage, restore namespace/final newline, align tests | first action; solution builds and every Domain test executes |
| TD-002 | P0 | current full solution build is red even though Application 1/1 and Integration 3/3 pass | use full build/test evidence, never describe partial projects as green | Phase 1 start; clean build and all projects execute |
| TD-003 | P1 | singular Domain feature folders, plural Application folder, lowercase `common`, inconsistent `*Test` naming | adopt plural feature folders/namespaces, PascalCase Common, mirrored `*Tests` before more features | early Phase 1; paths/namespaces consistent |
| TD-004 | P1 | Domain exposes actual `_members` List as `IReadOnlyCollection`; caller can downcast and mutate | return read-only wrapper/defensive view; test bypass is impossible | Phase 1 before repository mapping |
| TD-005 | P1 | Household can be constructed empty; handler adds Owner later, so persisted-owner invariant is not guaranteed by Domain | make aggregate creation include initial Owner, or explicitly prove another invariant-safe factory design | Phase 1 before initial migration |
| TD-006 | P1 | no maximum Household name in Domain; future database/API limits would disagree | select one limit (recommended 100) and enforce in Domain, API response semantics, and database | Phase 1 before migration |
| TD-007 | P1 | untracked CreateHousehold handler has no behavior tests | add recording-repository Application tests for orchestration, result, cancellation, and failure | first new file after green baseline |
| TD-008 | P1 | repository port does not state whether AddAsync tracks or commits | define success as durable single-aggregate commit; implementation calls one SaveChangesAsync | Phase 1 repository step |
| TD-009 | P1 | DbContext is empty; two untracked configuration files exist but are zero-byte, and an untracked Add+Save repository exists but is unregistered/unmapped/untested | inspect and complete the user-owned mappings; inspect/register/test the repository; add private-field materialization and fresh PostgreSQL migration proof | Phase 1 exit gate |
| TD-010 | P1 | no household endpoint/DTO/handler registration; existing OpenAPI has only health/readiness | add explicit DTO/ProblemDetails contract and an authenticated Testing-only route; prove Production 404 until Identity activates it | Phase 1 exit gate |
| TD-011 | P0/P1 | `CreatorUserId` is in the current internal command and would permit actor confusion if retained at an HTTP boundary | command accepts Name only; handler injects Application `ICurrentUser`; scoped Api adapter fails closed; real test scheme in Testing and Identity before exposure | Phase 1 boundary, Phase 2 public readiness |
| TD-012 | P1 | Domain `User` contains username/email but ASP.NET Core Identity will own credential identity | do not persist it in Phase 1; in Phase 2 remove/rename to UserProfile only if a business profile use case exists | before Identity migration |
| TD-013 | P1 | `CreateHouseholdHandler` converts a domain failure to `InvalidOperationException`; invalid Name has no defined 400 path | use an explicit Application validation outcome/stable code and map only it to 400; keep unexpected exceptions generic 500 | before first product endpoint |
| TD-014 | P1 | Domain timestamp test uses `Thread.Sleep(1)` | remove/rewrite deterministically; introduce clock/time boundary only with time behavior | Phase 1 |
| TD-015 | P1 | `Members`/name/enum decisions are not represented as database constraints | composite membership uniqueness, FK, length, role, and timestamp configuration in migration | Phase 1 |
| TD-016 | P1 | root/Application/Domain/architecture READMEs still say no product features exist | update only after the first slice is proven; describe exact current state | Phase 1 after green proof |
| TD-017 | P1 | README installs floating global `dotnet-ef 10.*` | checked-in local tool manifest with verified concrete version | before first migration |
| TD-018 | P1 | no CI; local restore/build/test/format/migration evidence can drift | GitHub Actions restore, build, unit, PostgreSQL integration, format, migration; NuGet audit connected | Phase 1 exit gate |
| TD-019 | P1 | audit-time format check reported 30 whitespace/final-newline diagnostics | run formatter/review changes after preserving user work; keep CI format gate | Phase 1 before merge |
| TD-020 | P1 | Compose PostgreSQL publishes on all host interfaces and `.env` documentation can imply ASP.NET loads it | bind local port to 127.0.0.1 and explain Compose `.env` versus .NET configuration | Phase 1 local hardening |
| TD-021 | P1 | authentication, authorization, rate limiting, token/cookie mode, and IDOR tests absent | implement Identity in Phase 2 and household resource authorization in Phase 3; do not expose multi-user routes early | before multi-user features |
| TD-022 | P1 | no production Dockerfile, deployment, migration job, secret store, least privilege, backup/restore, or observability | implement only during Phase 6 against working product | before public beta/production per Security Roadmap |
| TD-023 | P1 | Phase 2 Identity shape could create unused global role tables or omit Identity model configuration | use roleless `IdentityUserContext<ApplicationUser, Guid>` unless globally scoped roles are proven; call `base.OnModelCreating` before product configurations | Phase 2 migration |
| TD-024 | P1 | standard Identity normalized-email index is not a guaranteed uniqueness/concurrency boundary; Phase 1 member IDs can break a later Identity FK | set `RequireUniqueEmail`; named unique filtered NormalizedEmail constraint plus recognized `23505` mapping/concurrent test; choose purge/backfill/defer for FK and upgrade-test populated Phase 1 | Phase 2 exit gate |
| TD-025 | P1 | accepting an invitation mutates separate Invitation and Household aggregates while Phase 1 repositories auto-commit independently | one transaction port binds verified target, conditionally consumes exactly one pending invitation, mutates Household, and commits once; unique token hash/different-actor/concurrency/rollback tests | Phase 3 before invitation endpoint |
| TD-026 | P1 | last-owner rule has no concurrency enforcement mechanism | add Household version token touched by every membership mutation; map loser to 409; barrier-test demote/remove/leave/transfer | Phase 3 exit gate |
| TD-027 | P1 | ephemeral container keys or deleted wrapping-key versions would invalidate auth/confirmation/reset tokens across replicas, revisions, or rotation | Blob key ring, stable app name, versionless Key Vault key ID with old versions retained, least-privilege identity, and continuity/rotation tests | before public beta |
| TD-028 | P1 | naive deploy-then-migrate order/shared environments can expose incompatible code or data, while early contract migration defeats retained-image rollback | isolate environments; additive schema, backfill/dual compatibility, zero-traffic smoke, shift/observe, close rollback window, then contract via private-network runner | before public beta |
| TD-029 | P1 | routine occurrence materialization in a read query would violate the side-effect-free Today contract | derive virtual occurrences on GET; persist only through an explicit idempotent command with `(RoutineId, OccurrenceDate)` uniqueness | Phase 4 exit gate |
| TD-030 | P1 | “one active shopping list” is only a Domain intention and can race | unique HouseholdId/partial active-list constraint plus concurrent-create PostgreSQL test | Phase 5 exit gate |

## Current acceptable shortcuts

These are deliberate and should not be “cleaned up” during Phase 1 unless their trigger occurs.

| ID | Shortcut | Why acceptable now | Revisit trigger |
|---|---|---|---|
| AS-001 | raw Guid IDs | simple, interoperable, and low confusion with few modules | repeated wrong-ID-type bugs justify strongly typed IDs |
| AS-002 | `Guid.NewGuid()` in Domain | no deterministic/external-ID requirement | ID policy becomes business-relevant or deterministic tests require it |
| AS-003 | `DateTime.UtcNow` in simple creation/update behavior | timestamps are simple; an abstraction now is mostly ceremony | Phase 4 recurrence/deadline/DST tests need controlled time; prefer DateTimeOffset/time port then |
| AS-004 | simple Owner/Member/Guest enum | small closed role set and readable policies | roles gain independent behavior/configuration/lifecycle; do not turn Parent/Child into auth roles |
| AS-005 | small non-generic `Result` | one Domain operation uses success/failure; no HTTP contract depends on it | several use cases need typed stable error/value semantics |
| AS-006 | exceptions for constructor programmer/input guards | low number of cases and no product endpoint yet | expected client/business errors need explicit stable mapping |
| AS-007 | one DbContext, database, schema, and migration chain | simplest transactions/deployment for a small monolith | real module migration/ownership conflicts or isolation requirement |
| AS-008 | four layer assemblies with feature folders | compiler enforces layers and team/product is small | repeated cross-module reference violations cannot be contained by review/tests |
| AS-009 | concrete handler without interface/MediatR | explicit, testable, and no dispatch pipeline need | repeated proven pipeline behavior makes mediation cheaper than direct calls |
| AS-010 | repository commits single-aggregate write | clear transaction for first slice; DbContext already Unit of Work | Phase 3 invitation acceptance triggers one use-case-specific transaction port, not a generic UnitOfWork |
| AS-011 | direct DbContext in readiness endpoint | tiny technical health behavior in Api/Infrastructure composition | richer dependency health/metrics need ASP.NET health-check abstractions |
| AS-012 | per-test-class Testcontainer | only one integration class currently | multiple classes materially slow CI; then extract shared fixture/reset |
| AS-013 | one active shopping list per household, database-enforced | minimum usable shopping behavior | validated demand for stores/multiple lists/archive |
| AS-014 | read-only virtual routine occurrence derivation plus explicit idempotent interaction command | avoids GET side effects and worker/platform complexity | occurrences/notifications must happen durably without a user request |
| AS-015 | no API version prefix | no released incompatible client exists | mobile clients must coexist across a breaking contract |
| AS-016 | development credentials in example/development configuration | disposable local defaults, not secrets | any nonlocal environment; then managed secret/config provider is mandatory |

## Do not fix yet

These items are intentionally absent. Adding them now would create complexity rather than repay debt.

| Candidate | Decision now | Evidence required before reconsideration |
|---|---|---|
| BaseEntity/AggregateRoot base class | do not add | multiple entities share meaningful behavior, not merely Id/timestamps |
| generic repository | do not add | no expected trigger; persistence ports should speak aggregate/use-case language |
| UnitOfWork wrapper around EF | do not add | a real multi-repository transaction cannot be represented through one Infrastructure port |
| MediatR/CQRS framework | do not add | repeated cross-cutting handler pipeline with measured maintenance value |
| AutoMapper | do not add | explicit mapping repetition becomes a demonstrated defect/cost |
| FluentValidation | do not add | complex reusable conditional validation across many endpoints |
| strongly typed ID framework | do not add | repeated cross-ID mistakes and migration/API cost justified |
| universal clock/ID interfaces | do not add | time/ID behavior needs deterministic replacement; recurrence likely triggers clock first |
| Domain Service | do not add | a stateless business rule spans concepts and fits no entity/value object |
| Domain Events | do not add | one committed fact has multiple independent reactions and direct orchestration is coupled |
| event bus/outbox | do not add | reliable post-commit automatic work must survive process failure/retry |
| Kafka/RabbitMQ/MassTransit | do not add | durable cross-process work requires independent scaling/deployment |
| event sourcing | do not add | legal/business requirement for complete historical reconstruction |
| Redis/distributed cache | do not add | measured database bottleneck plus correct invalidation strategy |
| background scheduler/Hangfire/Quartz | do not add | work must happen without request/lazy creation and has durable retry semantics |
| WebSockets/SignalR | do not add | user research requires sub-second collaboration; polling/refetch is insufficient |
| per-module project/schema/database | do not add | current folders/tests cannot protect demonstrated module ownership boundaries |
| microservices/Kubernetes/service mesh | do not add | independent team/deploy/scaling requirements exceed lost simplicity |
| multi-region/replicas/partitioning | do not add | measured scale plus defined RTO/RPO and budget |
| general recurrence/RRULE engine | do not add | validated patterns exceed daily/selected weekdays and product accepts complexity |
| external calendar sync | do not add | validated demand and provider/security/consent scope |
| children/school/pickup/expenses/meals modules | do not add | core household coordination has usage evidence and the specific problem is validated |
| coverage percentage target | do not add | no trigger; tests should prove important behavior, not getters |

## Package/tooling maintenance notes

Audit-time checks reported no known vulnerable direct/transitive packages. Updates are still available and must be handled as a verified set, not automatic churn:

- EF Core Design patch level trails the ASP.NET Core package patch level.
- the current xUnit 2 package is marked legacy by NuGet while xUnit v3 exists.
- newer test SDK/runner versions exist.

Do not mix upgrades into the first persistence implementation unless a compatibility/security need requires it. Use a separate maintenance change with restore, clean build, migration generation, fresh PostgreSQL tests, and Testcontainers verification. Keep vulnerability auditing enabled in connected CI; advisory freshness is time-dependent.

## Debt review cadence

Review this register at every phase exit:

1. close only items with evidence.
2. add newly accepted shortcuts with owner, trigger, and latest gate.
3. promote an acceptable shortcut to P1 only when its trigger has occurred.
4. reject “cleanup” that introduces a deferred abstraction without evidence.
5. update repository paths/evidence after renames; do not leave stale debt entries.

## Phase 1 debt burn order

1. TD-001/002 — restore compilation and honest green proof.
2. TD-003–006 — freeze naming and aggregate/database invariants.
3. TD-007/008 — test Application and define commit boundary.
4. TD-009/015/017 — EF mappings, constraints, pinned migrations.
5. TD-010/011/013 — safe HTTP boundary and identity source.
6. TD-014/019 — deterministic/format quality.
7. TD-016/018/020 — documentation, CI, and local configuration accuracy.

Do not begin TD-021 multi-user behavior until the Phase 1 exit gate and Phase 2 Identity work are complete.
