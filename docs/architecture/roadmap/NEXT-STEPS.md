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
pending model changes, and the final Phase 1 run passed 57/57 tests plus
dependency audit, formatting, fresh-migration, and pending-model gates. Phase 1
is complete.

**Next implementation task:** begin Phase 2 Identity as a separately scoped
task. No Phase 2 source or product feature is included in this completion pass.

## Phase 1 status summary

| Step | Status | Evidence | Remaining |
|---|---|---|---|
| 1 | **COMPLETE** | ADR 0006 is explicitly Accepted and the implemented model follows its Account/Membership decision | linking, last-Owner concurrency, Account validation, authorization, and history are accepted follow-up work, not reasons to reopen the decision |
| 2 | **COMPLETE** | restore and full solution build passed with 0 warnings and 0 errors; Domain 30/30, Application 8/8, Integration 19/19 | keep the baseline reproducible after each change |
| 3 | **COMPLETE** | bounded normalized Name, initial Owner, generated MembershipId, loginless members, scoped duplicate checks, a non-downcastable live read-only Members view, and deterministic Domain tests exist | no Phase 1 aggregate-invariant work remains |
| 4 | **COMPLETE** | explicit Success/Invalid/Unauthenticated outcomes, trusted current Account, normalized success data, cancellation forwarding, zero-write invalid/unauthenticated tests, null-command behavior, and persistence-failure propagation are proven | no Phase 1 Application-boundary work remains |
| 5 | **COMPLETE** | active mappings, repository registration, local dotnet-ef 10.0.4 pin, two migrations, fresh-database migration via Testcontainers, and no pending model changes | no Phase 1 schema work remains |
| 6 | **COMPLETE** | PostgreSQL proves save/clear/reload, identity and role preservation, loginless members, scoped uniqueness including stale aggregates, exactly one asynchronous `SaveChanges` call, cancellation with zero rows, and rollback with zero partial rows | stable conflict mapping is deferred to the first membership mutation use case with a meaningful expected conflict; `CreateHousehold` has none |
| 7 | **COMPLETE** | Testing-only authenticated POST proves 201, controlled 400, anonymous/malformed 401 with zero rows, ignored caller AccountId, and Production 404 | production authentication and route activation are deliberately Phase 2 work |
| 8 | **COMPLETE** | the HTTP-to-PostgreSQL path, reload, zero-row expected failures, Testing OpenAPI 201/400/401, safe generic 500 non-disclosure, migration from zero, no model drift, Production non-exposure, and scoped stale-aggregate uniqueness are proven | last-Owner and linking concurrency belong to later membership mutation use cases |
| 9 | **COMPLETE** | `.github/workflows/backend-ci.yml` covers pinned SDK/tools, restore, connected vulnerability audit, build, all tests, format, fresh PostgreSQL migration, and pending-model verification; the complete local equivalent is green | hosted execution is not verified until these user-owned changes are committed and pushed |

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
against the current working tree: 30 Domain, 8 Application, and 17 Integration
tests; 55 total, 0 failed, with 0 build warnings and 0 build errors.

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
test authentication, map expected outcomes to RFC 7807 ProblemDetails with
stable code/traceId, and never serialize Domain/EF/Identity entities.

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
The full local equivalent is green. A hosted run is **NOT VERIFIED** until the
user-owned working-tree changes are committed and pushed.

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
