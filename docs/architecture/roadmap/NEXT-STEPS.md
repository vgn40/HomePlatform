# HomePlatform Next Steps

Status: **Authoritative executable order**  
Last reviewed: **2026-08-29**  
Strategic parent: [HomePlatform Six-Month Masterplan](HOMEPLATFORM-6-MONTH-MASTERPLAN.md)

## Current stop-gate

The working tree contains user-owned modified and untracked source work. Before
implementation, re-check branch/HEAD/status and preserve that work. Do not
stash, reset, clean, or overwrite it as part of these steps.

Source inspection found a constructor/caller/test mismatch, but this
documentation task did not run a fresh build. More importantly,
[ADR 0006](../adr/0006-separate-account-and-household-membership-identity.md)
is **Proposed** and changes the identity that the first migration would make
durable.

**Next document to review:** ADR 0006.  
**Next code file:** none until ADR 0006 is explicitly accepted or rejected.

After the decision:

- if accepted, open
  `backend/src/HomePlatform.Domain/Household/HouseholdMember.cs` first;
- if rejected, update the target/domain docs with the approved alternative,
  then open `backend/src/HomePlatform.Domain/Household/Household.cs` to restore
  constructor/caller/test alignment.

Do not create EF mappings or a migration before this branch is resolved.

## Step 1 — Decide Account versus Membership identity

### Review

- [ADR 0006](../adr/0006-separate-account-and-household-membership-identity.md)
- [Domain model](../target/DOMAIN-MODEL.md)
- [Context map](../target/CONTEXT-MAP.md)
- [Competitor impact on DDD](../research/COMPETITOR-IMPACT-ON-DDD.md)
- [Aggregate pressure test](../research/AGGREGATE-PRESSURE-TEST.md)

### Required outcome

Explicitly accept, reject, or revise the pre-schema answers for:

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
model, security plan, debt register, and this sequence agree. Documentation
maintenance alone must not mark the ADR Accepted.

## Step 2 — Restore a trustworthy green baseline

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

Only after Steps 1–4:

- complete the existing Household and HouseholdMember EF configurations;
- activate them in HomePlatformDbContext;
- map keys, lengths, timestamps, private collection, roles, nullable Account
  link, scoped uniqueness, and concurrency exactly as approved;
- register the existing focused Household repository;
- pin a concrete `dotnet-ef` version in `.config/dotnet-tools.json`;
- generate and review one migration containing only the approved Household
  schema.

Do not add Identity tables, User/Profile tables, Tasks, or future modules to the
first migration.

Complete when a fresh PostgreSQL database reaches the expected schema from
migration zero and EF reports no pending model changes.

## Step 6 — Prove repository and materialization semantics

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
