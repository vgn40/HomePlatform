# HomePlatform Next Steps

Status: **Authoritative executable order**  
Last reviewed: **2026-09-14**
Strategic parent: [HomePlatform Six-Month Masterplan](HOMEPLATFORM-6-MONTH-MASTERPLAN.md)

## Current stop-gate

The 2026-09-06 audit inspected `main@584bcaa` with existing uncommitted Account
Registration changes. Recheck branch/HEAD/status before implementation and
preserve concurrent work. No commit or push was made by the audit.

ADR 0006 is Accepted and its core Household identity model is implemented.
There are four migrations and dotnet-ef is pinned to 10.0.11. Phase 1's
2026-09-02 completion is historical; Phase 2 has started with roleless Identity
persistence, registration, bearer sign-in (`4180096`), and the implemented
Refresh endpoint. Renewed bearer tokens authenticate follow-up requests;
the remaining account lifecycle is incomplete.

### Account Registration: implemented and verified

Account Registration is committed in `e2fca98`. The historical registration baseline
was 72/72 tests: Domain 25, Application 11, Integration 36, with zero failures.
The complete HTTP -> Application -> Identity -> PostgreSQL slice includes:

- required/format/bounded email and required password validation, with zero
  writes for tested invalid inputs; Identity retains password-strength policy;
- Application-owned typed errors and ProblemDetails, plus generated 201/400
  OpenAPI contracts and the explicit registration response;
- sequential duplicates and a deterministic database race yielding one 201,
  one 400 EmailAlreadyExists, and one Account; only PostgreSQL UniqueViolation
  on UserNameIndex is translated, while unrelated failures remain safe 500.

### Account structure, bearer sign-in, and refresh

Application Accounts uses `Accounts/<UseCase>/` with `Register/`, `SignIn/`,
and `Refresh/`. Each operation owns its ports, results, and errors. Assemblies
represent layers; top-level Application/API folders represent business areas;
use-case folders represent operations. Infrastructure Identity adapters stay put.

`POST /api/accounts/register`, `POST /api/accounts/sign-in`, and
`POST /api/accounts/refresh` are implemented with AllowAnonymous in every
environment. Sign-in issues bearer access/refresh tokens and enforces Identity
lockout. Refresh follows this explicit slice:

```text
POST /api/accounts/refresh -> RefreshAccountRequest -> RefreshAccountCommand
-> RefreshAccountHandler -> IAccountRefresh -> IdentityAccountRefresh
-> BearerTokenHandler -> new AccessTokenResponse
```

RefreshAccountValidator requires a nonblank token. IdentityAccountRefresh
validates its expiry and the user's current security stamp before creating a
new principal and invoking the framework bearer handler. Successful refresh
returns new access and refresh tokens; Results.Empty prevents an extra endpoint
body. Missing/blank tokens return 400 RefreshTokenRequired; invalid, tampered,
expired, and stamp-invalidated tokens return 401 InvalidRefreshToken.

### Refresh verification — 2026-09-13

Against `main@1d36bf7` plus the existing uncommitted Refresh implementation and
new tests, restore/build passed with zero warnings/errors. All 105 tests passed:
Domain 25, Application 16, Integration 64; none skipped. The 19 added cases are
5 Application tests, 13 real-bearer PostgreSQL integration tests, and 1 generated
OpenAPI contract test. The separately filtered Refresh suite passed 18/18.
`git diff --check` passed. No production code changes were needed for this test
and documentation task; no commit or push was made.

Permanent coverage lives in
[RefreshAccountHandlerTests](../../../backend/tests/HomePlatform.Application.Tests/Accounts/RefreshAccountHandlerTests.cs),
[RefreshAccountEndpointTests](../../../backend/tests/HomePlatform.IntegrationTests/Accounts/RefreshAccountEndpointTests.cs),
and [OpenApiContractTests](../../../backend/tests/HomePlatform.IntegrationTests/OpenApiContractTests.cs).
It proves register -> sign-in -> refresh -> new access token -> protected
Household request -> persisted registered AccountId. It also covers missing,
null, empty, whitespace, malformed, and tampered tokens; changed Identity
security stamp; safe public ProblemDetails; a single JSON token response; and
anonymous refresh under an authenticated fallback policy. Expiry is controlled
with one test clock shared by the adapter and bearer-handler options, checking
one second before, exactly at, and one second after expiry without sleeps.
Generated OpenAPI declares only 200 AccessTokenResponse and 400/401 ProblemDetails.
Existing registration and bearer sign-in regressions remain green. This is
local test-host evidence; hosted CI, deployment, client token handling, and the
remaining lifecycle/release gates are not certified by it.

### Account-reference integrity verification — 2026-09-14

**IMPLEMENTED / VERIFIED locally** at `main@bc03a00` plus the user-owned
uncommitted mapping/migration and test changes. The nullable
`HouseholdMember.AccountId -> AspNetUsers.Id` FK uses EF `ClientNoAction` and
PostgreSQL `NO ACTION`. Migration `20260913183105_AddHouseholdMemberAccountReference`
adds the AccountId lookup index and FK without CASCADE or SET NULL. The separate
index supports AccountId lookup across Households; the existing unique index
starts with HouseholdId and retains its scoped uniqueness role.

[AccountReferenceIntegrityTests](../../../backend/tests/HomePlatform.IntegrationTests/Households/AccountReferenceIntegrityTests.cs)
cover real Identity links, loginless Member/Guest, invalid references,
tracked/untracked deletion for Owner/Member/Guest, an unreferenced Account's
deletion, and the optional EF FK (12 cases).
[AccountReferenceMigrationTests](../../../backend/tests/HomePlatform.IntegrationTests/Households/AccountReferenceMigrationTests.cs)
add two PostgreSQL upgrade cases from `20260904100319_AddIdentityPersistence`:
valid data survives and the named FK rejects deletion; dangling data makes the
migration fail with unchanged memberships, AccountIds, Accounts and migration
history. Invalid historical data must not be silently deleted, nulled or repaired
with invented Accounts. Existing Household integration fixtures seed real
Identity Accounts.

Build passed with zero warnings/errors. The full solution passed **119/119**:
Domain 25, Application 16, Integration 78; **0 skipped**.
`git diff --check` passed. Production mapping, migration, designer and snapshot
were reviewed read-only and preserved byte-for-byte. This is local test evidence;
deployed migration state and actual historical data remain NOT VERIFIED. Before
applying to an existing environment, inspect for dangling references and resolve
any invalid rows explicitly under the adopted lifecycle decisions.

### Structural refactor verification — 2026-09-09

Against `main@4180096` plus the structural refactor and unchanged uncommitted
Refresh files, solution restore and build passed with zero warnings/errors.
All 86 tests passed: Domain 25, Application 11, Integration 50; none skipped.
`git diff --check` passed. This is local verification, not hosted CI or
deployment evidence.

### Now: adopted privacy/lifecycle implementation order

The 2026-09-13 documentation task began with a clean `main@07eeb12`. The current
decisions are recorded in [DELETION-DESIGN.md](../../privacy/DELETION-DESIGN.md),
with the [ADR 0006 addendum](../adr/0006-separate-account-and-household-membership-identity.md#current-decision-addendum--2026-09-13).
This order supersedes the earlier “remaining account lifecycle, then
collaboration” sequence; historical Phase 1/Refresh evidence below is retained.

| Order | Work | Status / gate |
|---|---|---|
| 1 / Phase A | Privacy/lifecycle decisions documented | ADOPTED; unresolved policy questions remain OPEN in the canonical design |
| 2 / Phase B | Account-reference integrity | IMPLEMENTED / VERIFIED locally: nullable FK, AccountId lookup index, real Identity-seeded fixtures, tracked/untracked constraint proof and valid/dangling migration upgrades |
| 3 / Phase C | Required ownership lifecycle support | NEXT CODE PHASE, NOT YET IMPLEMENTED: start TransferOwnership test-first, then required LeaveHousehold/CloseHousehold work with authorization, invariants and concurrency proof |
| 4 / Phase D | Protected-request Account validity | NOT YET IMPLEMENTED: current Account existence/validity; Household access also checks current membership |
| 5 / Phase E | DeleteAccount | NOT YET IMPLEMENTED: ownership resolved across all Households, all Account-linked memberships explicitly deleted without loginless conversion, unresolved last Owner refused, lifecycle and Identity deletion atomic, future protected requests denied |
| 6 / Phase F | ExportMyData / rectification / ChangeEmail and wider privacy rights | Later; resolve scope, authority, email lifecycle and legal questions |
| 7 / Phase G | Production privacy/security gates | Before real-user release: retention/backups/restore, providers/regions, operational proof and remaining security controls |

**TransferOwnership is the next CODE feature, starting test-first.** Begin with
Household ownership lifecycle tests for an explicitly selected eligible
Account-linked destination, authorization, no automatic promotion, no loginless
Owner and a continuing Account-linked Owner. Resolve destination acceptance and
exact API/concurrency choices when designing that feature; they remain OPEN.
Require PostgreSQL rollback/race proof for its durable implementation. No
TransferOwnership production code or future-feature tests are added in this
Account-reference review.

DeleteAccount membership fate is now **RESOLVED**: after ownership resolution,
explicitly delete every Account-linked membership across all Households, then
Identity/ApplicationUser and Account-owned data. Do not convert memberships to
loginless; nullable AccountId remains for separate loginless-member flows.
Last Owner requires explicit transfer to an eligible Account-linked person or
closure, never automatic promotion. This does not decide standalone
LeaveHousehold or future feature lifecycles.

Each persisted feature must complete the
[lifecycle checklist](../../privacy/DELETION-DESIGN.md#feature-lifecycle-checklist)
before its design is complete, including shared-record survival, unnecessary
personal-reference removal and person-dependent deletion. Do not use a generic
usage heuristic or retain personal data “just in case”. Do not introduce a
grace period or generic privacy/session/OAuth framework.

### Remaining security and collaboration gates

Confirmation, recovery, logout/revocation, prior-refresh-token reuse, rate
limiting, client token storage, Data Protection continuity, public auth error
policy and the independent NormalizedEmail lifecycle remain required gates.
The exact access lifetime is OPEN; it cannot substitute for immediate denial
after committed DeleteAccount. Preserve the current UserName=email uniqueness
guard until an explicit email/username lifecycle decision replaces it.

Dependency tests still need framework/package and project-reference guards.
Account-reference integrity is verified locally. Keep Household creation
Testing-only until current-Account validation, resource authorization and the
remaining release gates pass. Registration/sign-in/refresh source
exposure in Production is not deployment evidence. Broader invitations and
collaboration follow their security gates; lifecycle primitives needed for
DeleteAccount are deliberately brought forward.

The [repo audit](../DDD-ARCHITECTURE-AUDIT.md) owns point-in-time evidence. The
following Phase 1 acceptance detail and historical test counts do not certify
the new lifecycle decisions or their implementation.

## Phase 1 status summary — historical 2026-09-02 evidence

| Step | Status | Evidence | Remaining |
|---|---|---|---|
| 1 | **COMPLETE** | ADR 0006 is explicitly Accepted and the implemented model follows its Account/Membership decision | linking, last-Owner concurrency, Account validation, authorization, and history are accepted follow-up work, not reasons to reopen the decision |
| 2 | **COMPLETE** | restore and full solution build passed with 0 warnings and 0 errors; Domain 30/30, Application 8/8, Integration 19/19 | keep the baseline reproducible after each change |
| 3 | **COMPLETE** | bounded normalized Name, initial Owner, generated MembershipId, loginless members, scoped duplicate checks, a non-downcastable live read-only Members view, and Domain behavior tests exist | no Phase 1 aggregate-invariant work remains |
| 4 | **COMPLETE** | explicit Success/Invalid/Unauthenticated outcomes, trusted current Account, normalized success data, cancellation forwarding, zero-write invalid/unauthenticated tests, null-command behavior, and persistence-failure propagation are proven | no Phase 1 Application-boundary work remains |
| 5 | **COMPLETE** | active mappings, repository registration, then-current local dotnet-ef 10.0.4 pin, two migrations, fresh-database migration via Testcontainers, and no pending model changes | no Phase 1 schema work remains |
| 6 | **COMPLETE** | PostgreSQL proves save/clear/reload, identity and role preservation, loginless members, scoped uniqueness including stale aggregates, exactly one asynchronous `SaveChanges` call, cancellation with zero rows, and rollback with zero partial rows | stable conflict mapping is deferred to the first membership mutation use case with a meaningful expected conflict; `CreateHousehold` has none |
| 7 | **COMPLETE** | Testing-only authenticated POST proves 201, controlled 400, anonymous/malformed 401 with zero rows, ignored caller AccountId, and Production 404 | production authentication and route activation are deliberately Phase 2 work |
| 8 | **COMPLETE** | the HTTP-to-PostgreSQL path, reload, zero-row expected failures, Testing OpenAPI 201/400/401, safe generic 500 non-disclosure, migration from zero, no model drift, Production non-exposure, and scoped stale-aggregate uniqueness are proven | last-Owner and linking concurrency belong to later membership mutation use cases |
| 9 | **COMPLETE** | `.github/workflows/backend-ci.yml` covers pinned SDK/tools, restore, connected vulnerability audit, build, all tests, format, fresh PostgreSQL migration, and pending-model verification; the complete local equivalent is green | current hosted execution is NOT VERIFIED by this audit |

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

**Status: COMPLETE (historical).** The initial 2026-09-02 baseline passed
30 Domain, 8 Application, and 17 Integration tests: 55 total, 0 failed, with
0 build warnings and 0 build errors. The later Phase 1 contract/quality pass
increased that historical suite to 57; the current audit has its own test count.

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

**Status: COMPLETE.** The implemented and tested rules cover bounded normalized
Name, invariant-safe initial Owner, generated Membership identity, loginless
members, and duplicate non-null Account prevention. `Members` exposes a live
read-only view that cannot be downcast to the backing mutable `List`, and the
timestamp test no longer depends on `Thread.Sleep`.

Implement and test only the approved minimum:

- bounded nonblank Household Name;
- invariant-safe initial Owner Membership;
- no duplicate non-null Account link within one Household under accepted ADR 0006;
- member collection cannot be mutated outside aggregate behavior;
- stable Membership identity; later verified link behavior has its own gate;
- no family relationship implicitly grants authority.

Avoid `Thread.Sleep` in Domain tests. Do not add base entity, aggregate-root,
clock, ID framework, Domain Service, or Domain Event without a proven behavior.

Complete when Domain tests express the exact approved invariants and no
persistence-specific public mutation was added.

## Step 4 — Complete the Application use case before persistence

**Status: COMPLETE.** The handler returns explicit `Success`, `Invalid`, and
`Unauthenticated` outcomes. Application tests prove normalized success data and
the trusted Account as initial Owner; invalid and unauthenticated requests write
nothing; null commands still throw `ArgumentNullException`; cancellation is
forwarded; and unexpected repository failures propagate. The API maps these
expected outcomes to 201, 400, and 401 respectively.

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
registered; the then-current dotnet-ef pin was 10.0.4 (now 10.0.11); `InitialHousehold` and
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

**Status: COMPLETE for the CreateHousehold slice.** Real PostgreSQL tests prove
save/clear/reload, field-backed materialization, preserved HouseholdId, Name,
MembershipId, AccountId, and Owner role, loginless members, and the scoped unique
constraint, including contention between stale aggregate instances. An
interceptor proves exactly one asynchronous `SaveChanges` invocation per
repository `AddAsync`; an already-cancelled write leaves zero Household and
HouseholdMember rows; and a forced member-insert failure rolls the transaction
back with no partial aggregate rows.

Use a reusable PostgreSQL/Testcontainers fixture. Prove:

- exactly one `SaveChangesAsync` for the one-aggregate write;
- aggregate save, tracker clear, and reload preserve private state;
- constraints reject invalid/scoped duplicates;
- cancellation/failure rolls back;
- when a use case has a meaningful expected PostgreSQL conflict, map that
  conflict alone to a stable Application outcome;
- no SQLite or EF InMemory substitute is treated as proof.

Complete when handler success means a durable PostgreSQL commit and private
Domain state materializes without public persistence setters.

`CreateHousehold` has no meaningful expected database conflict: it creates a new
aggregate with generated identities, and duplicate Account contention is a
membership mutation concern. Provider/infrastructure failures therefore remain
unexpected and propagate for this slice. Stable conflict mapping is not deleted;
it is deferred to the first Account-linking or membership-mutation use case that
can define a business-level conflict outcome. The stale-aggregate uniqueness test
remains proof that PostgreSQL enforces the underlying scoped invariant.

## Step 7 — Add a Testing-only HTTP boundary

**Status: COMPLETE.** The request contains Name only; the Testing host installs
a real authenticate/challenge scheme; the handler receives a server-derived
Account; integration tests prove 201, invalid-name 400, anonymous/malformed 401
with zero rows, caller AccountId non-impersonation, and Production 404.

Create explicit request/response DTOs, current-account adapter, and
`POST /api/households` route. The request contains only `name`. Require real
test authentication, map validation to ProblemDetails, and never serialize
Domain/EF/Identity entities. Stable product code/traceId consistency remains a
production-contract follow-up; the Phase 1 tests do not prove it.

Keep the product route Testing-only until Phase 2 Identity is active.
Development/Production must not expose a fake-auth endpoint.

Complete when anonymous/malformed subject requests are 401 with zero rows,
valid trusted actor requests are 201, invalid names are controlled 400s, and an
extra caller-supplied actor field cannot impersonate another Account.

## Step 8 — Prove the full slice and critical contracts

**Status: COMPLETE for the CreateHousehold slice.** Testing OpenAPI directly
proves the route's operation ID, tag, 201/400/401 responses, and created-response
schema. A normally authenticated HTTP request with an overridden throwing
repository proves an unexpected infrastructure failure becomes a Problem
Details 500 without exception, provider, SQL, stack, or connection disclosure.

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

The stale-aggregate PostgreSQL uniqueness test is the applicable cardinality
and contention proof for CreateHousehold. Last-Owner, Account linking/unlinking,
invitation consumption, and membership role mutation do not exist in this use
case and remain gated under their later use cases; they do not keep Step 8 open.

## Step 9 — Put the evidence in CI

**Status: COMPLETE for the repository gate.** The workflow is discoverable at
`.github/workflows/backend-ci.yml` and covers the pinned SDK/tool restore,
solution restore, connected dependency audit, deterministic build, all tests,
format verification, fresh PostgreSQL migration, and pending-model verification.
The full local equivalent was green at the Phase 1 snapshot. A current hosted
run is **NOT VERIFIED** by this audit; workflow presence does not prove execution.

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

Continue the active Phase 2 work in the order at the top of this document.
Do not build invitations, Tasks, Shopping, or a public client against fake
authentication. The detailed 2026-08-27 sequence is
retained as [historical planning evidence](../../research/archive/planning-baseline-2026-08-27/NEXT-10-STEPS.md),
not current authority.
