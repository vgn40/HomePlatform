# HomePlatform Deletion Design

Status: **ADOPTED decisions; Account FK and Household lifecycle primitives IMPLEMENTED; DeleteAccount NOT IMPLEMENTED in committed main**

Decision date: **2026-09-13**

Current source review: **2026-09-19**, committed `main@1ca1dd0`. Separate
uncommitted DeleteAccount work is excluded. No runtime tests were rerun.

Historical initial decision evidence: `main@07eeb12`; source inspection on 2026-09-13.
Historical Account-reference review: **2026-09-14**, `main@bc03a00` plus the user-owned
uncommitted implementation and tests; local PostgreSQL/build evidence below.
No deployed database or real-user data was inspected.

## Purpose

This is the canonical source for Account deletion, Household lifecycle,
ownership, shared-data boundaries, database guardrails, and access after
deletion. It complements [ADR 0006](../architecture/adr/0006-separate-account-and-household-membership-identity.md).
An Owner is a Household role, not ownership of another person's personal data.

Status vocabulary across the privacy documents:

- **ADOPTED:** a product/domain rule or architecture direction has been decided.
- **PROPOSED:** a candidate requiring a later decision; not a binding rule.
- **OPEN:** no final decision exists.
- **IMPLEMENTED:** present in the stated committed source snapshot.
- **VERIFIED locally:** executed checks establish behavior only for the dated
  source/run stated; this does not certify current HEAD, deployment or operations.
- **NOT YET IMPLEMENTED:** the required behavior is absent; adopting a rule
  does not prove runtime enforcement.

The task brief supplies the adopted decisions and the prior audit's requested
privacy-document set. No standalone HomePlatform privacy audit was found in
the repository. The initial supporting documents use the brief and live
repository evidence; they do not infer missing audit findings or legal facts.

## Person target and revised hardening priority — 2026-09-20

[ADR 0007](../architecture/adr/0007-person-as-stable-human-identity.md) accepts
Person as stable human identity separate from credentials and memberships.
Person/Relationship are TARGET; CareCircle is FUTURE. None is implemented.
The current Account-linked deletion/no-conversion policy remains adopted for
AS-IS. It does not decide future Person survival, deletion, relationship fate
or care access. Design those lifecycles before the affected Person migration;
do not infer an Account-to-Person cascade or automatic retention.

The earlier immediate post-deletion access-denial rule is superseded by the
explicit deferral in [Authentication After Deletion](#authentication-after-deletion).
Refresh denial remains separate. NEXT-STEPS prioritizes product/domain work;
this document preserves lifecycle and release requirements, not another schedule.
Uncommitted DeleteAccount source includes transaction/rollback code and tests;
its atomicity, ownership resolution and readiness are not verified by this task.

## Adopted Decisions

1. Account and Household have separate lifecycles. DeleteAccount,
   LeaveHousehold, TransferOwnership, and CloseHousehold are distinct operations.
2. Membership does not imply ownership or administrative authority. A
   HouseholdMember never automatically becomes Owner because someone leaves.
3. TransferOwnership explicitly selects a concrete eligible Account-linked
   person. Acceptance by the destination Owner is **OPEN**.
4. HouseholdRole is independent of Account presence: a Person without an
   ApplicationUser may be Owner. Any number of Owners is supported. This does
   not change the separately defined DeleteAccount membership lifecycle.
5. A continuing Household must retain an Owner. A departing last Owner must
   explicitly transfer ownership or close the Household before Account deletion.
6. A Household used only by the departing Account may be explicitly closed
   within DeleteAccount when no other person's/shared data needs to continue.
   Clearly inform the user that Household data will be deleted. No grace period
   is adopted; do not build one without a later concrete product decision.
7. Loginless HouseholdMembers can represent real people, including children.
   A null AccountId does not mean anonymous or outside deletion/privacy scope.
8. Account deletion must not automatically cascade-delete a Household that
   other Account-linked people legitimately continue using. Shared record
   ownership is distinct from Account ownership.
9. Require a real nullable `HouseholdMember.AccountId -> AspNetUsers.Id` foreign
   key with rejecting/guarding deletion behavior. IMPLEMENTED / VERIFIED locally
   on 2026-09-14; see Database Guardrails.
10. Do not use Account-to-HouseholdMember `CASCADE DELETE` or automatic
    `SET NULL` as lifecycle logic. Resolve relations explicitly first.
11. **Revised 2026-09-20 — DEFERRED TECHNICAL DEBT:** immediate denial of an
    already-issued access token is not currently required before product work.
    Record its possible validity until expiry, decide the release risk, and
    separately require deleted/invalid-Account Refresh denial.
12. DeleteAccount is normal product lifecycle; an Article 17 erasure request
    is a separate privacy/legal request with potentially different scope.
    Reuse concrete deletion logic where appropriate, without a generic
    GdprService/PrivacyService.
13. **Membership fate is RESOLVED for DeleteAccount:** after ownership rules
    are resolved, explicitly delete every HouseholdMember linked to the deleted
    AccountId across all Households, then delete Identity/ApplicationUser and
    Account-owned data. Never silently convert those memberships to loginless.
14. Deleting an Account and its memberships does not automatically delete every
    record it created or interacted with. Shared records follow explicit
    feature/data-type lifecycle rules; remove unnecessary personal references
    from records that survive under those rules.
15. Every persisted feature must define scope, personal references,
    LeaveHousehold, DeleteAccount, CloseHousehold and surviving/person-dependent
    record behavior before its design is lifecycle-complete.
16. Do not retain personal data “just in case”. Without a documented continuing
    purpose, delete the relevant personal data by default. Do not infer a
    cascade deletion of legitimate Household-shared records from creator
    deletion; a feature-specific lifecycle decision is required.

## Account vs Household Lifecycle

| Operation | Scope and adopted boundary | Implementation status |
|---|---|---|
| DeleteAccount | Resolve ownership rules, explicitly delete all Account-linked HouseholdMember memberships across all Households, then delete Identity/ApplicationUser and Account-owned data; no conversion to loginless or implicit Household closure | NOT YET IMPLEMENTED |
| LeaveHousehold | Depart from one Household while preserving Account and other memberships | IMPLEMENTED, Testing-only: removes caller Member/Guest or Owner when another Owner remains; refuses only the last Owner with 409, including the sole member |
| TransferOwnership | Explicitly choose an eligible Account-linked destination; no automatic promotion | IMPLEMENTED, Testing-only: current Owner demoted to Member, non-Owner target promoted to Owner; other Owners and IDs preserved; already-Owner target rejected; destination acceptance and concurrency remain open |
| CloseHousehold | Explicitly end a Household, separately from Account deletion | IMPLEMENTED, Testing-only: any Owner authorizes physical Household deletion; all its memberships cascade-delete, Accounts survive; future record-type policies remain open |

The three Household HTTP contracts are recorded in
[TARGET-ARCHITECTURE](../architecture/target/TARGET-ARCHITECTURE.md#current-architecture).
Their Domain, handler, persistence and endpoint tests exist; no new passing
runtime result is claimed here. DeleteAccount membership fate is
**ADOPTED: delete, never automatically convert to loginless**. This does not
decide concrete future feature lifecycles. Current standalone LeaveHousehold
allows an Owner to leave when another Owner remains. The last Owner must
explicitly transfer or close; Leave never silently deletes a Household.
A stable MembershipId does not mandate retaining a Membership or personal
attribution after deletion.

## DeleteAccount Across Multiple Households

One Account can have memberships in multiple Households. Read and resolve
**all** memberships for its AccountId, never only the client-selected active or
current Household. Evaluate the Account's role independently in each Household.

For example, if Account A has memberships in Household 1, Household 2 and
Household 3, DeleteAccount must resolve ownership and delete all three linked
memberships. None is silently retained with a null AccountId.

| Situation in one Household | Required resolution |
|---|---|
| Member or Guest | Delete the Account-linked membership as part of DeleteAccount; do not change another person's role |
| Owner with another Owner | Household continues; delete the departing Account's membership without changing any other person's role |
| Last Owner | Explicit TransferOwnership to an eligible Account-linked person, or explicit CloseHousehold, before deleting the Account-linked membership |
| No memberships | No Household resolution is needed before the remaining Account deletion work |

An unresolved last-Owner case blocks DeleteAccount. Revalidate the full plan
and relevant invariants when committing so concurrent membership/Owner changes
cannot bypass them. The approved ownership/shared-data lifecycle, deletion of
all Account-linked memberships and Identity deletion must succeed atomically
or leave no partial deletion. Exact concurrency and transaction mechanics are
**OPEN implementation details**.

## Ownership Rules

- Member, Guest, partner, grandparent, or child status never implicitly grants
  Owner authority. There is no “last Owner disappears, pick another Member” rule.
- TransferOwnership must identify a concrete eligible destination with a valid
  Account link. Loginless HouseholdMembers cannot be that destination.
- Household supports one, two or more Owners, including Persons without an
  ApplicationUser. AddMember accepts every valid HouseholdRole and preserves
  existing Owners. Person identity remains separate from Account identity.
- Leave checks that another Owner remains before removing an Owner. A rejected
  last-Owner leave preserves membership state and UpdatedAt, including when
  that Owner is the sole member. This aggregate check is not concurrency proof.
- Destination acceptance is **OPEN** and may become a separately decided flow.

## Last Owner Flow

The user must resolve each last-Owner Household with explicit transfer or
closure. A Household with only the departing Account may be closed as part of
DeleteAccount only with clear deletion information and the explicit choice.
Counting Account-linked members alone is insufficient: check for loginless
people and shared data as well. A Household never continues ownerless.

```mermaid
flowchart TD
    A[DeleteAccount request] --> B[Read all memberships for AccountId]
    B --> C{Unresolved Household?}
    C -->|Yes| D{Owner?}
    D -->|No| E[Plan Account-linked membership deletion]
    D -->|Yes| F{Another Owner remains?}
    F -->|Yes| G[Plan membership deletion without changing other roles]
    F -->|No| H{Explicit last Owner choice?}
    H -->|TransferOwnership| I[Validate concrete eligible Account-linked destination]
    H -->|CloseHousehold| J[Confirm closure and data consequences]
    H -->|Unresolved| K[Refuse DeleteAccount]
    I --> L[Resolve transfer then plan membership deletion]
    J --> M[Resolve closure including membership deletion]
    E --> C
    G --> C
    L --> C
    M --> C
    C -->|No| N[All memberships resolved]
    N --> O[Revalidate and apply approved lifecycle atomically]
    O --> R[Explicitly delete all Account-linked memberships]
    R --> P[Delete Identity Account and commit]
    P --> Q[Refresh denied; access may remain valid until expiry]
```

The diagram describes required behavior, not existing code. Invalid choices or
unresolved policies cannot advance to commit. Resolution steps prepare the
approved deletion plan; they do not authorize partial commits per Household.
No arrow performs automatic Owner promotion.

## Loginless Members and Children

`HouseholdMember.AccountId` remains conceptually nullable: loginless membership
creation/management is a separate supported domain flow. DeleteAccount deletes
existing Account-linked memberships; it does not silently turn them into
loginless memberships or require AccountId to become non-nullable.

`AccountId = null` expresses identity separation, not anonymization. A loginless
HouseholdMember may represent a child or another real person even though the
current model stores no name, birth date, or family-relationship field on it.

If the last Owner deletes their Account and loginless people remain, transfer
to an eligible Account-linked person or explicitly close the Household. Never
promote a loginless person or leave an ownerless Household. CloseHousehold must
handle their data under the approved privacy/shared-data policy. The final
child-account policy and any unresolved detailed data fate remain **OPEN**.

## Shared Data

**ADOPTED:** Account and membership deletion does not automatically delete all
records the Account created or interacted with. “Created by an Account” does
not establish Account ownership. Classify the record and its person references
separately, including across multiple Households:

| Category | Adopted lifecycle boundary |
|---|---|
| Account-owned data | Email, username, password hash and security/account state are deleted with Identity/ApplicationUser |
| Membership/person-specific data | Every HouseholdMember linked to the deleted AccountId is deleted after ownership resolution; other person-specific records follow their explicit feature lifecycle |
| Household-scoped/shared domain data | A record may survive where an explicit feature/data-type rule establishes a continuing legitimate Household product purpose |
| Personal references inside shared data | Remove references to the deleted person that are no longer necessary, even when the shared record survives |

Data about other people, including loginless children, must still be assessed
separately from the departing person's data. Owner is a Household authority
role, not ownership of another person's personal data. **Shared record
ownership ≠ Account ownership.**

Decide lifecycle explicitly per feature/data type. Do not use a generic runtime
heuristic such as “Does someone still use this?” to decide which records
survive. If no continuing purpose for personal data is documented, delete that
personal data by default; do not retain it “just in case”. This is not a rule
to automatically cascade-delete legitimate shared records when their creator
is deleted. An undecided feature lifecycle is an incomplete design, not a
license to invent retention or cascade behavior at runtime.

### Personal references in surviving records

Remove unnecessary person references under the feature's lifecycle rule.
Define how missing attribution is presented; do not retain a name/email merely
to show who the deleted person was or store a fake replacement identity.

**ILLUSTRATIVE / NOT YET ADOPTED FEATURE RULE:** a future Task could retain its
Household content after removing creator attribution:

| Field | Before | After |
|---|---|---|
| HouseholdId | H1 | H1 |
| Title | Køb mælk | Køb mælk |
| CreatedByMembershipId | Membership being deleted | null |

The UI could display **“Tidligere medlem”** for missing attribution. By default,
that is UI presentation, not a stored replacement identity. Tasks are NOT YET
IMPLEMENTED; this example does not adopt a Task lifecycle, field/nullability
mapping, or assignment policy.

Use **“personal reference removed”**, **“personreference fjernet”** or
**“attribution removed”** for this outcome. Setting AccountId, MembershipId or
CreatedBy to null does not by itself establish anonymous data. Only claim
anonymization when it has been verified that the remaining data cannot identify
the person directly or indirectly, including through free text or other links.
Nulling a reference is not a general conclusion about GDPR compliance.

### Every persisted feature must define its lifecycle

**ADOPTED engineering/product rule:** the design of every new feature with
persisted data must answer the following before it is lifecycle-complete:

| Design question | Required decision |
|---|---|
| Scope / ownership | Classify the record as primarily Account-, Membership/person-, or Household-scoped; creator identity alone does not decide ownership |
| Personal references | Identify person fields/links, including CreatedByMembershipId, AssignedToMembershipId, ParticipantMembershipId, AccountId and free-text attribution where relevant |
| LeaveHousehold | Define what happens when the person leaves one Household but retains their Account |
| DeleteAccount | Define what happens when the Account and its linked memberships disappear from every Household |
| CloseHousehold | Define what happens when the Household itself closes |
| Surviving shared record | Retain the record under the explicit feature rule, remove unnecessary personal references, define missing-attribution UI, and review export/retention/privacy consequences |
| Person-dependent record | Delete the record under its feature lifecycle when it has no continuing product purpose without the person |

### Feature lifecycle checklist

For each new persisted feature:

- [ ] Record scope classified: Account / Membership / Household; Person/Relationship when introduced
- [ ] Personal-reference fields identified
- [ ] LeaveHousehold behavior defined
- [ ] DeleteAccount behavior defined
- [ ] CloseHousehold behavior defined
- [ ] Missing/deleted-person attribution UI defined if relevant
- [ ] Export impact reviewed
- [ ] Retention impact reviewed
- [ ] Privacy notice/data inventory impact reviewed
- [ ] Deletion/integration tests planned
- [ ] Shared records do not expose removed personal references unintentionally

A feature containing persisted personal/shared data is not lifecycle-complete
until these questions have been answered. Planning tests here does not claim
that deletion behavior exists or has been verified.

### Future feature examples

**ILLUSTRATIVE / NOT YET ADOPTED FEATURE RULES.** These features are NOT YET
IMPLEMENTED; concrete scope, attribution/history and lifecycle remain OPEN.

| Feature | Possible direction to assess during feature design |
|---|---|
| Shopping | ShoppingList may be Household-scoped; creator deletion need not delete the list, while personal creator attribution may be removable |
| Events | An Event may be Household-scoped with removable creator attribution; the departing person's participation is a separate person-specific relationship |
| Tasks | A Task may survive creator deletion with creator attribution removed; assignment to the departing person requires an explicit Task lifecycle rule |
| Routines | Scope may be Household- or person-oriented; do not decide it before Routines is designed |

Concrete attribution/history rules and immediate hard deletion of every future
record type on CloseHousehold remain **OPEN**. See the
[data inventory](DATA-INVENTORY.md) and [retention policy](RETENTION-POLICY.md).

## Database Guardrails

**IMPLEMENTED; historical local verification on 2026-09-14:** [HouseholdMemberConfiguration](../../backend/src/HomePlatform.Infrastructure/Persistence/Configurations/HouseholdMemberConfiguration.cs)
maps nullable `AccountId -> AspNetUsers.Id` with EF `DeleteBehavior.ClientNoAction`.
The [migration](../../backend/src/HomePlatform.Infrastructure/Persistence/Migrations/20260913183105_AddHouseholdMemberAccountReference.cs),
its designer and [model snapshot](../../backend/src/HomePlatform.Infrastructure/Persistence/Migrations/HomePlatformDbContextModelSnapshot.cs)
agree. The migration adds `IX_HouseholdMember_AccountId` and
`FK_HouseholdMember_AspNetUsers_AccountId` with PostgreSQL `NO ACTION`, without
CASCADE or SET NULL. The AccountId index supports cross-Household Account lookup;
the existing `(HouseholdId, AccountId)` unique index retains its separate scoped
uniqueness role. Domain remains independent of EF and Identity types.

[AccountReferenceIntegrityTests](../../backend/tests/HomePlatform.IntegrationTests/Households/AccountReferenceIntegrityTests.cs)
prove valid Identity references, null Member/Guest links, rejection of unknown
Accounts, tracked/untracked deletion guards for Owner/Member/Guest, deletion of
an unreferenced Account and optional FK metadata (12 cases).
[AccountReferenceMigrationTests](../../backend/tests/HomePlatform.IntegrationTests/Households/AccountReferenceMigrationTests.cs)
prove upgrade from `20260904100319_AddIdentityPersistence`: valid data remains
intact and the FK is enforced; dangling data makes the migration fail with
unchanged memberships, AccountIds, Accounts and migration history. It does not
silently null a link, delete a membership or invent an Account. Inspect actual
existing rows before an environment upgrade; those rows and deployment state
are NOT VERIFIED by local tests. In that historical run, build passed without
warnings/errors; the full
solution passed **119/119: Domain 25, Application 16, Integration 78; 0 skipped**.

**ADOPTED; DeleteAccount NOT YET IMPLEMENTED:** explicitly delete all memberships
linked to that AccountId after ownership resolution and before Identity-account
deletion. Do not automatically convert them to loginless memberships. Nullable
AccountId remains supported for separately created/managed loginless members.
The guarding FK does not implement ownership lifecycle or protected-request
current-Account validation.

Do not use Account-to-HouseholdMember `CASCADE DELETE`: it can blindly remove
membership/shared identity. Do not use automatic `SET NULL` to substitute for
an explicit Account/Person lifecycle. Database behavior cannot choose domain ownership.

The current [HouseholdConfiguration](../../backend/src/HomePlatform.Infrastructure/Persistence/Configurations/HouseholdConfiguration.cs)
has a separate Household-to-HouseholdMember cascade. Identity's own dependent
tables also have cascade relationships. These are existing schema facts, not
an Account-to-HouseholdMember cascade or authorization to close a Household.
The approved CloseHousehold flow must assess its own data consequences.

## Authentication After Deletion

**DEFERRED TECHNICAL DEBT — revised decision 2026-09-20:** an already-issued
short-lived access token may remain usable until expiry after Account deletion
unless immediate server-side revocation/current-Account validation is implemented.
This explicitly replaces the earlier immediate-denial requirement as a universal
development prerequisite. Exact lifetime and acceptable production exposure are
OPEN and must be reviewed before release; no immediate revocation is claimed.

**AS-IS:** [HttpCurrentAccount](../../backend/src/HomePlatform.Api/Identity/HttpCurrentAccount.cs)
parses authenticated subject claims; the
[bearer registration](../../backend/src/HomePlatform.Infrastructure/DependencyInjection.cs)
adds no current-Account check. The Account FK guards data references, not access
tickets. Household membership/role checks remain required and independently
restrict access after membership removal.

**Refresh is separate:** [IdentityAccountRefresh](../../backend/src/HomePlatform.Infrastructure/Identity/IdentityAccountRefresh.cs)
validates expiry and the current user's security stamp. Deleted/invalid Accounts
must not renew access. The existing check is not proof of completed DeleteAccount
integration or of revoking an already-issued access token. When reviewing that
work, prove Refresh denial and record the residual access window explicitly.

Introduce focused current-Account validation or revocation when concrete product
risk, a feature dependency or a release requirement demands immediate cutoff.
[TD-020](../architecture/roadmap/TECHNICAL-DEBT-REGISTER.md) and the
[security roadmap](../architecture/roadmap/SECURITY-ROADMAP.md) retain that work;
no generic session/OAuth framework is required.

## GDPR Erasure Relationship

**ADOPTED product distinction:** DeleteAccount is the normal Account lifecycle.
An Article 17 erasure request is a privacy/legal request about personal data;
its scope can differ and include a person without an Account. Article 17 has
conditions and exceptions; a product button does not settle their application.
See [GDPR Article 17](https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng).

Reuse concrete deletion logic where the approved outcomes match, while allowing
different entry points and identity/authority verification. Do not create a
generic GdprService/PrivacyService or treat the last-Owner product flow as a
complete legal-request procedure. Exact Article 6 basis per processing
activity remains **OPEN** in the [processing register](PROCESSING-REGISTER.md).

## Open Decisions

DeleteAccount membership fate is **RESOLVED**: delete every Account-linked
membership after ownership resolution, with no automatic loginless conversion.
The following separate decisions remain open:

| Decision | Status / implementation gate |
|---|---|
| Destination Owner acceptance | OPEN; explicit transfer is adopted, an acceptance mechanism is not |
| Attribution/history in future shared records | OPEN per record type; unnecessary personal-reference removal is ADOPTED, precise fields/history behavior is not |
| Concrete Tasks/Shopping/Events/Routines lifecycles | OPEN: scope, LeaveHousehold/DeleteAccount/CloseHousehold behavior and feature-specific hard-delete rules require feature design |
| DeleteAccount reauthentication UX | OPEN before exposing DeleteAccount |
| Exact access-token lifetime / revocation | OPEN; document acceptable expiry window for release, or require immediate cutoff based on risk |
| Person lifecycle / Account–Person link | OPEN before affected migration: Person survival/deletion, management authority, relationship fate and cross-Household access; current membership policy does not answer these |
| Retention periods | OPEN; no settled durations |
| Backup retention | OPEN; no settled duration or restore/deletion procedure |
| Final child-account policy | OPEN; loginless people still require privacy handling |
| Exact Article 6 legal basis per processing activity | OPEN; no basis finalized here |
| Hosting/provider/region decisions | OPEN; earlier Azure designs are PROPOSED options |
| Immediate hard deletion of every future record type by CloseHousehold | OPEN; explicit closure does not settle all future data policies |
| Precise concurrency strategy | OPEN; no Household concurrency token on main; atomicity and preservation of invariants remain required |
| Standalone Owner leave | Owner may leave when another Owner remains; last Owner must transfer or explicitly close |

There is no adopted grace period. Do not add one without a later concrete
product decision. Resolve the relevant open decisions before implementing the
affected behavior; do not invent defaults while coding.

## Implementation Sequence

The [next steps](../architecture/roadmap/NEXT-STEPS.md) own executable ordering.
These phase labels describe lifecycle dependencies and acceptance gates, not
an alternative execution order or a claim that every primitive is absent.

| Phase | Scope | Required evidence / gate |
|---|---|---|
| A — docs/decisions | Record the adopted lifecycle and preserve open questions | This document and active product/architecture/roadmap docs agree; no runtime completion claim |
| B — Account reference integrity | IMPLEMENTED / VERIFIED locally: nullable FK, AccountId index, ClientNoAction / NO ACTION and real Identity-seeded fixtures | 12 integrity cases plus 2 valid/dangling upgrade cases; actual environment data still requires inspection before upgrade |
| C — Household lifecycle primitives | IMPLEMENTED through Testing-only HTTP/Application/Domain/persistence: transfer, Member/Guest or non-last-Owner leave and any-Owner close; concurrency and broader authorization proof remain open | Explicit destination/closure, no automatic promotion, Account-independent Owner role, no continuing ownerless Household, rollback/race proof |
| D — DEFERRED immediate current-Account validation | Trigger on concrete immediate-cutoff or release risk; current membership checks remain required | Prove unexpired-token denial if selected; otherwise explicitly document acceptable access expiry window; removed membership cannot retain Household access |
| E — DeleteAccount | Resolve ownership across all Households, refuse unresolved last-Owner cases, explicitly delete every Account-linked membership without loginless conversion, atomically apply approved lifecycle and delete Identity Account | Multi-Household membership deletion/no-conversion, ownership success/refusal, rollback, concurrency, feature-specific reference cleanup, existing loginless-person cases, post-commit Refresh denial and the chosen access-token policy |
| F — wider privacy rights | ExportMyData, rectification/ChangeEmail, wider request handling | Decide scope/verification and remaining legal questions; protect other people's data |
| G — operations/release | Retention, backups/restore, provider/region decisions, production privacy/security gates | Validate real operational controls before real-user release; documentation and local tests alone are insufficient |

**NEXT:** follow NEXT-STEPS for the incremental Person foundation. Pull lifecycle
hardening and review of existing DeleteAccount work forward when its concrete
feature dependency, risk or release exposure warrants it. Existing
lifecycle primitives and historical local FK verification do not close the
remaining policy or race-safety gates. Phase G is the final release gate, not permission to process real-user data before privacy/security review.
No new generic privacy, transaction, session, or OAuth framework is planned.
