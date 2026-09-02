# HomePlatform Next Steps

Status: **Authoritative executable order**  
Last reviewed: **2026-09-02**
Strategic parent: [HomePlatform Six-Month Masterplan](HOMEPLATFORM-6-MONTH-MASTERPLAN.md)

## Current stop-gate

The 2026-09-02 working tree remains user-owned and dirty. Re-check
branch/HEAD/status before implementation and do not stash, reset, clean, or
overwrite concurrent work.

ADR 0006 is explicitly **Accepted**, the Account/Membership model is implemented
for the CreateHousehold slice, two reviewed migrations exist, EF reports no
pending model changes, and a fresh restore/build/test run passed 50/50 tests.
The current gate is narrower: finish the remaining Step 3 encapsulation/test
cleanup, make the public error/OpenAPI contract provable, put the workflow in a
CI-discoverable location, and make formatting verification green before
declaring Phase 1 complete.

**Next implementation task:** complete the two remaining Step 3 items without
introducing a clock abstraction: prevent callers from downcasting `Members` to
the backing mutable list, and remove the `Thread.Sleep`-based Domain test.

## Phase 1 status summary

| Step | Status | Evidence | Remaining |
|---|---|---|---|
| 1 | **COMPLETE** | ADR 0006 is explicitly Accepted and the implemented model follows its Account/Membership decision | linking, last-Owner concurrency, Account validation, authorization, and history are accepted follow-up work, not reasons to reopen the decision |
| 2 | **COMPLETE** | restore and full solution build passed; Domain 28/28, Application 8/8, Integration 14/14 | keep the baseline reproducible after each change |
| 3 | **PARTIALLY COMPLETE** | bounded normalized Name, initial Owner, generated MembershipId, loginless members, scoped duplicate checks, and Domain tests exist | exposed collection is still downcastable to its backing `List`; one Domain test uses `Thread.Sleep` |
| 4 | **PARTIALLY COMPLETE** | Name-only command, trusted current-account port, cancellation forwarding, zero-write validation tests, and persistence-failure propagation exist | expected validation/unauthenticated failures are still exception-shaped rather than stable Application outcomes; no Application-level failing-current-account test |
| 5 | **COMPLETE** | active mappings, repository registration, local dotnet-ef 10.0.4 pin, two migrations, fresh-database migration via Testcontainers, and no pending model changes | no Phase 1 schema work remains |
| 6 | **PARTIALLY COMPLETE** | PostgreSQL save/clear/reload and scoped uniqueness tests pass; the repository performs one aggregate add and one `SaveChangesAsync` by inspection | no instrumented exactly-once SaveChanges proof, cancellation/rollback integration test, or stable conflict mapping |
| 7 | **COMPLETE** | Testing-only authenticated POST proves 201, controlled 400, anonymous/malformed 401 with zero rows, ignored caller AccountId, and Production 404 | production authentication and route activation are deliberately Phase 2 work |
| 8 | **PARTIALLY COMPLETE** | the HTTP-to-PostgreSQL happy path, reload, zero-row expected failures, migration from zero, no model drift, and Production non-exposure are proven | no HTTP-exposed OpenAPI document currently includes the Testing-only route, and no document-generation test proves it; unexpected error non-disclosure and remaining concurrency contracts are not tested |
| 9 | **PARTIALLY COMPLETE** | a workflow definition with restore/build/test/format/migration/model checks exists under `github/workflows` | GitHub discovers workflows only under `.github/workflows`; no clean CI run is evidenced, and local formatting verification currently fails |

## Step 1 — Decide Account versus Membership identity

**Status: COMPLETE.** ADR 0006 was intentionally accepted on 2026-08-30. The
current Domain, Application, persistence, and Testing-host slice align with its
core identity decision. Acceptance does not claim that all follow-up use cases
or concurrency invariants are implemented.

### Review

- [ADR 0006](../adr/0006-separate-account-and-household-membership-identity.md)
- [Domain model](../target/DOMAIN-MODEL.md)
- [Context map](../target/CONTEXT-MAP.md)
- [Competitor impact on DDD](../research/COMPETITOR-IMPACT-ON-DDD.md)
- [Aggregate pressure test](../research/AGGREGATE-PRESSURE-TEST.md)

### Required outcome

The accepted pre-schema answers are:

- stable Membership identity;
- optional verified Account link;
- loginless participant;
- several Household Memberships per Account;
- scoped duplicate rule;
- authorization role versus relationship;
- retained assignment/history identity;
- link/unlink authority and last-Owner behavior.

### Complete when

ADR status and consequences are explicit, and target architecture, domain
model, security plan, debt register, and this sequence agree. The acceptance
predates this reconciliation and is not inferred from source code.

## Step 2 — Restore a trustworthy green baseline

**Status: COMPLETE.** On 2026-09-02 the exact restore/build/test sequence passed
against the current working tree: 28 Domain, 8 Application, and 14 Integration
tests; 50 total, 0 failed.

### Inspect in order

1. `backend/src/HomePlatform.Domain/Household/HouseholdMember.cs`
2. `backend/src/HomePlatform.Domain/Household/Household.cs`
3. `backend/src/HomePlatform.Application/Households/CreateHousehold/CreateHouseholdHandler.cs`
4. `backend/tests/HomePlatform.Domain.Tests/Household/HouseholdTests.cs`
5. `backend/tests/HomePlatform.Domain.Tests/Household/HouseholdMembersTest.cs`

Align the approved Membership model, Household creation, handler call, and
tests. Keep Owner/Member/Guest as authorization roles unless the decision
explicitly changes them. Do not mix folder/namespace renames into semantic
repair unless the changed paths are stable and reviewed.

### Verify

```bash
dotnet restore backend/HomePlatform.slnx
dotnet build backend/HomePlatform.slnx --no-restore --disable-build-servers -m:1
dotnet test backend/HomePlatform.slnx --no-build --disable-build-servers -m:1
```

### Complete when

The full solution builds and every test project executes. Partial project tests
do not establish the baseline.

## Step 3 — Protect the approved Household invariants

**Status: PARTIALLY COMPLETE.** The implemented and tested rules cover bounded
normalized Name, invariant-safe initial Owner, generated Membership identity,
loginless members, and duplicate non-null Account prevention. The public
`IReadOnlyCollection` currently returns the backing `List`, so callers can still
downcast and mutate it. `AddMember_updates_updated_at` still uses
`Thread.Sleep(1)`.

Implement and test only the approved minimum:

- bounded nonblank Household Name;
- invariant-safe initial Owner Membership;
- no duplicate active Account link within one Household if ADR 0006 is
  accepted;
- member collection cannot be mutated outside aggregate behavior;
- stable Membership identity and link behavior if approved;
- no family relationship implicitly grants authority.

Avoid `Thread.Sleep` in Domain tests. Do not add base entity, aggregate-root,
clock, ID framework, Domain Service, or Domain Event without a proven behavior.

Complete when Domain tests express the exact approved invariants and no
persistence-specific public mutation was added.

## Step 4 — Complete the Application use case before persistence

**Status: PARTIALLY COMPLETE.** The handler, Name-only command,
`ICurrentAccount`, focused repository port, hand-written tests, cancellation
forwarding, validation zero-write behavior, and failure propagation exist.
Expected validation and unauthenticated conditions remain exception-shaped,
and the Application suite does not directly test a failing current-account
port with zero repository writes.

Create Application tests before changing infrastructure:

1. add hand-written recording-repository tests for CreateHousehold;
2. add a narrow `ICurrentAccount` or equivalently named Application port;
3. remove `CreatorUserId` from `CreateHouseholdCommand`;
4. inject the trusted actor into the handler;
5. define stable success, validation, and unauthenticated outcomes;
6. make repository success mean one durable aggregate commit.

Required cases: exactly one aggregate captured, initial Owner matches the
trusted Account/Membership decision, cancellation is forwarded, invalid input
and missing actor perform zero writes, and persistence failure is not false
success.

Complete when Application behavior is green without EF or HTTP.

## Step 5 — Map the approved model and pin migrations

**Status: COMPLETE.** EF configurations are active, the focused repository is
registered, dotnet-ef 10.0.4 is pinned, `InitialHousehold` and
`LimitHouseholdNameLength` are present, Testcontainers applies migrations from
zero, and EF reports no pending model changes.

Only after Steps 1–4:

- complete the existing Household and HouseholdMember EF configurations;
- activate them in HomePlatformDbContext;
- map keys, lengths, timestamps, private collection, roles, nullable Account
  link, scoped uniqueness, and concurrency exactly as approved;
- register the existing focused Household repository;
- pin a concrete `dotnet-ef` version in repository-root `dotnet-tools.json`;
- generate and review focused migrations containing only the approved Household
  schema and its bounded-name correction.

Do not add Identity tables, User/Profile tables, Tasks, or future modules to the
first migration.

Complete when a fresh PostgreSQL database reaches the expected schema from
migration zero and EF reports no pending model changes.

## Step 6 — Prove repository and materialization semantics

**Status: PARTIALLY COMPLETE.** Real PostgreSQL tests prove save/clear/reload,
field-backed materialization, stable `MembershipId`, loginless members, and the
scoped unique constraint. Source inspection shows one aggregate add followed by
one `SaveChangesAsync`. Cancellation/rollback behavior, an instrumented
exactly-once assertion, and stable expected-conflict mapping are not yet proven.

Use a reusable PostgreSQL/Testcontainers fixture. Prove:

- exactly one `SaveChangesAsync` for the one-aggregate write;
- aggregate save, tracker clear, and reload preserve private state;
- constraints reject invalid/scoped duplicates;
- cancellation/failure rolls back;
- expected PostgreSQL conflicts alone map to stable conflict outcomes;
- no SQLite or EF InMemory substitute is treated as proof.

Complete when handler success means a durable PostgreSQL commit and private
Domain state materializes without public persistence setters.

## Step 7 — Add a Testing-only HTTP boundary

**Status: COMPLETE.** The request contains Name only; the Testing host installs
a real authenticate/challenge scheme; the handler receives a server-derived
Account; integration tests prove 201, invalid-name 400, anonymous/malformed 401
with zero rows, caller AccountId non-impersonation, and Production 404.

Create explicit request/response DTOs, current-account adapter, and
`POST /api/households` route. The request contains only `name`. Require real
test authentication, map expected outcomes to RFC 7807 ProblemDetails with
stable code/traceId, and never serialize Domain/EF/Identity entities.

Keep the product route Testing-only until Phase 2 Identity is active.
Development/Production must not expose a fake-auth endpoint.

Complete when anonymous/malformed subject requests are 401 with zero rows,
valid trusted actor requests are 201, invalid names are controlled 400s, and an
extra caller-supplied actor field cannot impersonate another Account.

## Step 8 — Prove the full slice and critical contracts

**Status: PARTIALLY COMPLETE.** Most runtime-path evidence is green. OpenAPI is
mapped only in Development while the Household route is mapped only in Testing,
so no HTTP-exposed document currently describes this endpoint, and no direct
document-generation test proves it. Tests also do not prove that unexpected
persistence/provider failures avoid internal detail leakage, and only scoped
duplicate-link concurrency is covered.

Against the real Testing host and PostgreSQL, prove:

- one Household and initial Owner Membership persist atomically;
- saved aggregate reloads correctly;
- OpenAPI describes 201/400/401;
- Production host returns 404 for the test-only route;
- error responses reveal no stack/provider internals;
- migration from zero and pending-model checks pass;
- the ADR-selected identity/cardinality constraints behave under concurrency.

Complete when the entire HTTP -> Application -> Domain -> repository ->
PostgreSQL path is repeatable.

## Step 9 — Put the evidence in CI

**Status: PARTIALLY COMPLETE.** The intended workflow content exists, but its
current untracked path is `github/workflows/backend-ci.yml`, not GitHub Actions'
discoverable `.github/workflows/backend-ci.yml`. No clean hosted run is
evidenced. In addition, the local format gate fails on existing whitespace,
encoding, and final-newline findings.

Add one backend workflow that runs SDK setup, connected restore/audit, build,
Domain/Application tests, PostgreSQL integration tests, local EF tool restore,
fresh migration, pending-model check, and format verification.

```bash
dotnet restore backend/HomePlatform.slnx
dotnet build backend/HomePlatform.slnx --no-restore
dotnet test backend/HomePlatform.slnx --no-build
dotnet format backend/HomePlatform.slnx --verify-no-changes --no-restore
```

Complete when a clean environment reproduces the slice. Then update only
current-state statements; do not turn target claims into implemented claims.

## After the first slice

Begin Phase 2 Identity. Do not build invitations, Tasks, Shopping, or a public
client against fake authentication. The detailed 2026-08-27 sequence is
retained as [historical planning evidence](../../research/archive/planning-baseline-2026-08-27/NEXT-10-STEPS.md),
not current authority.
