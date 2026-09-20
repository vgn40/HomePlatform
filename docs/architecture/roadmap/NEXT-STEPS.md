# HomePlatform Next Steps

Status: **Sole authoritative execution order**
Last reviewed: **2026-09-20**, committed `main@7162c35`

## Product/domain direction

HomePlatform helps people coordinate shared life across households, relationships
and changing family structures. Account is credentials; Person is the human;
HouseholdMembership is participation. Person and Relationship are accepted
**TARGET** concepts, not implemented. CareCircle is **FUTURE**.

Product/domain development now leads. Known technical risks remain in the
[debt register](TECHNICAL-DEBT-REGISTER.md), [security roadmap](SECURITY-ROADMAP.md)
and [deletion design](../../privacy/DELETION-DESIGN.md), with triggers and release
requirements. They are not a blanket checklist before meaningful product work.

## Planned / Next

Work one bounded slice at a time. This table is the **sole execution order**;
other documents supply design constraints/evidence, not competing schedules.

| Order | Product/domain step | Boundary and completion evidence |
|---|---|---|
| 1 — documented | Record Person-centered identity decision | ADR 0007 accepts Person, Relationship and Person-based participation; AS-IS remains explicit; no code implementation claim |
| 2 — NEXT | Design the smallest incremental path to Person | Decide minimum data and authority, optional Account link, existing membership/backfill mapping, privacy/lifecycle and compatibility. Preserve MembershipIds where participation continues; do not infer matches between loginless records or rewrite the architecture |
| 3 | Implement minimal Person foundation | Only the agreed state/behavior and persistence; prove a stable Person can exist without credentials, with proportionate Domain/persistence tests |
| 4 | Evolve HouseholdMembership toward Person participation | Apply the designed migration; preserve existing Owner/Member/Guest authority, account eligibility and membership identity. Prove one Person can participate in distinct Households under explicit product rules without cross-Household access leakage |
| 5 | Deliver the first concrete flow proving Person independent of Account | Choose the smallest useful flow in step 2, for example representing/showing a loginless participant; carry it through authorized Application/API/persistence behavior with explicit lifecycle. This is a candidate, not an approved child-management API |
| 6 — conditional | Introduce Relationship for a real flow | Specify only the directionality, symmetry, taxonomy, lifecycle and permissions that flow needs; no Person inheritance or implicit authority |
| 7 — FUTURE, conditional | Introduce child/caregiver concepts only when required | CareCircle/CareCircleMembership remains beyond the Person foundation; do not design custody/medical features now |
| 8 — ongoing | Add product capabilities that validate the model | Select meaningful slices, potentially Tasks, Shopping, Events or other coordination needs; no breadth quota or fixed delivery calendar |

Step 1 is adopted by the documentation commit containing this roadmap. Steps
2–5 are incremental implementation work, not a giant migration project.
Conditional steps are not commitments to build unused concepts.
`HouseholdRelationship` is exploration only and is not an implementation item.

Before overlapping code changes, inspect the existing uncommitted DeleteAccount
work; do not overwrite it or create duplicate slices. Household authorization
and resource concealment are committed in `7162c35`.
This preserves concurrent work without making completion of all its hardening a
prerequisite to Person design/development.

## Deferred technical debt and release hardening

- **Current-Account validity (TD-020):** an already-issued short-lived access
  token may remain usable until expiry after Account deletion without immediate
  server-side revocation/current-Account validation. Refresh denial for a deleted
  or invalid Account remains a separate requirement. Revisit for a concrete
  immediate-cutoff need or release risk decision.
- **Household concurrency (TD-011/022):** retain stale-write/ownership race risks;
  pull forward for simultaneous mutations or stronger production guarantees.
- **DeleteAccount (TD-021):** review existing work when selected; membership
  resolution, atomicity/rollback, reauthentication and revocation remain open
  acceptance questions. Its presence is not proof of completion.
- **Read performance (TD-023):** projections, pagination, indexes, generated SQL
  and PostgreSQL query plans follow a concrete read-heavy feature.
- **HTTP/observability and DI/test/architecture review (TD-024/025/018):** strengthen
  real slices, consolidate when useful, and meet applicable release gates.
- **RELEASE GATE (TD-010/013):** product-first ordering does not authorize real-user
  exposure without applicable security, privacy and operational evidence.

When a trigger changes the priority, record the revised order here. Basic
correctness, trusted actors and authorization for a new slice are never optional.

## Open design decisions

- Minimum Person state, creation/edit authority, module/aggregate ownership,
  Account–Person linking mechanism and verification.
- Mapping/backfill of existing linked and loginless memberships, duplicate
  resolution without automatic matching, compatibility and rollback.
- Person lifecycle and Account deletion versus Person survival, cross-Household
  privacy/access, and minimum product rules for multiple memberships.
- Relationship directionality, symmetry, taxonomy, lifecycle/effective dates
  and explicit permissions; CareCircle details await a real flow.
- Existing lifecycle questions: Owner destination acceptance, standalone Owner
  leave, DeleteAccount reauthentication/atomicity, token lifetime and release
  revocation policy. See the canonical deletion design.

Keep the .NET four-layer modular monolith. No microservices, event sourcing,
mediator/generic repository framework, broker, graph database or generic
permission engine is justified by the Person decision.

## Evidence boundary

The preserved 2026-09-19 reconciliation inspected committed source, mappings,
all four migrations, endpoints, handlers, Domain behavior, repositories,
Identity, test sources and CI. This 2026-09-20 documentation update rechecked
HEAD/status/diff, current identity/lifecycle source and the documentation workflow.
Household authorization is committed locally in `7162c35` after the affected
117-test slice passed. The remote server was not fetched or verified, and
nothing was pushed. This source baseline excludes the documentation commit
containing this reconciliation; that commit changes Markdown only.

The working tree contains separate, uncommitted DeleteAccount production/test
work. Committed Household authorization now maps nonmember denials to 404.
DeleteAccount has
endpoint/handler/adapter and transaction/rollback/test code, but is not complete
or verified by this task. This work is not part of the committed AS-IS baseline
and was not changed by this review. Reconcile it before an overlapping implementation task. “Not implemented”
below means absent from this committed main snapshot, not absent from every
local file. Recheck HEAD and working-tree changes before continuing.

## AS-IS / Implemented

| Capability | Committed behavior | Boundary / remaining work |
|---|---|---|
| Accounts | Anonymous registration, bearer sign-in and refresh; roleless Identity in Infrastructure | Mapped in all environments; source exposure is not deployment evidence. Confirmation, recovery and logout/revocation flows remain unimplemented |
| Account/Membership separation | Stable MembershipId, nullable AccountId, Household Owner/Member/Guest roles | Verified link/unlink workflows and invitations are not implemented |
| Account reference | Nullable FK to AspNetUsers.Id, ClientNoAction / PostgreSQL NO ACTION, AccountId lookup index | Rejects unresolved Account deletion; does not validate every bearer request |
| CreateHousehold | Trusted actor becomes initial Owner; bounded name; aggregate persisted | Testing-only HTTP route |
| TransferOwnership | Owner selects an existing Account-linked Membership; target becomes Owner, caller becomes Member; IDs preserved | Completed HTTP/Application/persistence flow in `b2b3656`, after use case `4f829cd`; no destination acceptance or optimistic concurrency |
| LeaveHousehold | Member/Guest deletes their own membership; every Owner is refused with 409, even if another Owner exists | `539205f`; preserves Account, Household and other memberships |
| CloseHousehold | Owner authorizes physical Household deletion; its memberships cascade-delete, Accounts survive | `1ca1dd0`; no soft-close state or policies for unbuilt shared features |
| Authorization | RequireAuthorization plus fresh loaded-aggregate membership/Owner checks for transfer/leave/close | All four Household routes are Testing-only. Anonymous/invalid authentication returns 401; missing Household or nonmember returns 404; known Member/Guest lacking Owner authority returns 403. Transfer checks the actor before target validation; authorized Owner validation remains 400, and Owner leave remains 409 |
| Persistence | Tracked Include(Members) query; mutations committed through focused repository SaveChangesAsync | No Household concurrency token, paginated read projection or production query-performance evidence |
| HTTP infrastructure | AddProblemDetails, UseExceptionHandler, authentication then authorization; Development/Testing OpenAPI | Error-body/code/trace consistency remains incomplete |
| Tests and CI | Three xUnit projects; real PostgreSQL 18.6 Testcontainers; backend-ci.yml | Test source/workflow presence is not a current passing run |

The route and layer evidence is summarized in
[TARGET-ARCHITECTURE](../target/TARGET-ARCHITECTURE.md#current-architecture).
The [deletion design](../../privacy/DELETION-DESIGN.md) remains authoritative for
adopted lifecycle rules and open policy questions.

## Verified locally

**Household authorization — 2026-09-20:** the following command rebuilt the
live working tree before local commit `7162c35` and passed **117/117** tests:
Domain 36, Application 44, Integration 37; zero failed or skipped.

```bash
cd backend
dotnet test HomePlatform.slnx --disable-build-servers -m:1 \
  --filter "FullyQualifiedName~TransferOwnership|FullyQualifiedName~LeaveHousehold|FullyQualifiedName~CloseHousehold"
```

The run included the preserved DeleteAccount files in compilation, but selected
only the three Household test slices; it does not validate DeleteAccount.
Denial cases cover state preservation, outsider 404, wrong-role 403, Owner
validation and invalid-target concealment. This is local evidence, not a full
solution test run. The 2026-09-19 review remains historical source evidence.
Existing test sources
cover Domain invariants, handler outcomes, PostgreSQL persistence, lifecycle HTTP
contracts, Production route absence, Account FK upgrades and generated OpenAPI.
The transfer/leave/close endpoint suites use test authentication; the Account
sign-in/refresh suites separately exercise real bearer tokens. These are distinct
forms of coverage and do not establish a full real-bearer lifecycle matrix.

**Not verified in hosted CI:** no hosted run was inspected. The workflow defines
restore, dependency audit, build, test, formatting, fresh-database migration and
pending-model checks. Deployment, deployed migrations, real-user data, backup
restore and operational controls are **not verified**; no deployment is claimed.

### Refresh verification — 2026-09-13

**Historical local evidence:** `main@1d36bf7` plus then-uncommitted Refresh and
test changes: 105/105 passed (Domain 25, Application 16, Integration 64).
This is not the test count at `1ca1dd0`.

### Account-reference integrity verification — 2026-09-14

**Historical local evidence:** `main@bc03a00` plus then-uncommitted FK/migration
and test changes: 119/119 passed (Domain 25, Application 16, Integration 78),
zero skipped and zero build warnings/errors. Integrity implementation was later
committed in `b31ec55`. No deployed migration or existing-data audit is implied.

The [preserved 2026-09-14 snapshot](HISTORICAL-NEXT-STEPS-2026-09-14.md)
retains detailed Refresh, FK, structural-refactor and Phase 1 evidence. All
numbers and execution instructions there are historical.
