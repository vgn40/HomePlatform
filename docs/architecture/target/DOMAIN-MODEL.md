# HomePlatform Domain Model

> Ownership update — 2026-09-23 (local implementation, not a commit or deployment):
> a Household supports any number of Owners and must retain at least one while
> it exists. An Owner can add Member, Guest or Owner, including a Person without
> an ApplicationUser. Member/Guest and Owners with another Owner remaining may
> leave; only the last Owner receives 409 and must transfer or explicitly close.
> Transfer rejects an already-Owner target and changes only caller and target;
> its existing Application requirement for an Account-linked target is unchanged.
> Any Owner may close. These rules supersede older ownership statements below;
> concurrency protection remains open.

Status: **Authoritative current-versus-target DDD description**  
Last reviewed: **2026-09-20**; committed AS-IS `main@7162c35`, target accepted in ADR 0007

## Product and model direction

HomePlatform coordinates shared life across households, relationships and
changing family structures. **AS-IS** has Account-linked memberships.
**TARGET** adds Person and Relationship; **FUTURE** adds a possible care context.
No Person, Relationship or CareCircle type exists in current source.

## Current implemented state

The current Domain is small and still evolving. Create, transfer, leave and
close behavior is implemented; concurrency and broader authorization remain
incomplete. The affected authorization slice passed locally; see
[NEXT-STEPS](../roadmap/NEXT-STEPS.md#verified-locally) for exact scope:

| Concept | Current shape | Current status |
|---|---|---|
| Household | Guid identity, trimmed Name bounded to 100 characters, UTC timestamps, private member list | aggregate root for creation, ownership transfer, leave and close; creates one Account-linked Owner Membership |
| HouseholdMember | generated `MembershipId`, optional `AccountId`, HouseholdRole | implemented entity inside Household; persisted with scoped uniqueness; no link/unlink lifecycle behavior |
| HouseholdRole | Owner, Member, Guest enum | vocabulary, not a value object or domain service; transfer changes roles, leave/close check current authority |
| Account | Guid reference only in Household Domain | credentials belong to Infrastructure ApplicationUser/Identity; the former Domain User has been removed |
| Result / typed lifecycle results | success/failure primitive and operation-specific errors | boundary outcomes; neither entities, value objects, nor domain services |

Nonmember concealment is committed in `7162c35`. Separate DeleteAccount work
remains uncommitted and unchanged; it is not a completed/verified lifecycle baseline.

The current source, handler, mappings, migrations, and tests agree on the core
Account/Membership identity decision in accepted ADR 0006. PostgreSQL tests
prove persistence and materialization, but this is not a finished membership
lifecycle or authorization model.

## Implemented invariants

Source inspection shows these early rules:

- Household Name cannot be blank, is trimmed, and is limited to 100 characters;
- the Household constructor rejects an empty Owner AccountId and creates
  one Owner Membership;
- HouseholdMember generates MembershipId and rejects empty AccountId/undefined
  roles, including Owner with null AccountId;
- `AddMember` can create a loginless Membership and rejects duplicate linked
  AccountId within one Household instance;
- transfer requires an Owner actor and a different existing Account-linked
  target; it promotes the target, demotes the caller and preserves MembershipIds;
- leave removes only the caller's membership and rejects every Owner, including
  one of multiple Owners; it does not remove the Account;
- close authorizes only an Owner; the handler/repository performs physical
  deletion of the Household and all its memberships, preserving Accounts;
- member addition, transfer and leave update `UpdatedAt`;
- `Members` exposes a live read-only view that cannot be downcast to the backing
  mutable `List`;
- EF maps the private collection and PostgreSQL preserves Membership identity,
  nullable Account links, and scoped uniqueness.

Partially implemented or not yet proven:

- stable Membership identity is proven across persistence, but not across the
  unimplemented link/unlink lifecycle;
- later verified Account linking and unlinking;
- last-Owner behavior across every leave/remove/demote race;
- concurrency tokens and stable conflict mapping;
- full authorization policy/real-bearer lifecycle coverage. All four Household
  routes are Testing-only; transfer/leave/close already check membership/roles.

The private constructors are EF materialization hooks, not public ways to
construct invalid entities. Mappings preserve encapsulation, but EF and direct
database writes can bypass public constructor checks. The database has no role,
Owner-presence or nonblank-name checks. Account existence is guarded by the
nullable FK to AspNetUsers.Id. There is no public
child mutation API; future membership writes must remain aggregate-owned.

Current maturity is a domain model with dependency inversion and one explicit
aggregate (Level 2 overall, early Level 3 in Household). More contexts, events,
and distribution would not by themselves improve model quality.

## Ubiquitous language

| Term | Meaning | Must not mean |
|---|---|---|
| Account | credential-bearing authentication identity | a participant in every Household |
| Household | protected collaboration boundary | global family graph or authentication tenant |
| Person | TARGET stable identity of a human, with or without credentials | Account, Membership or a subtype such as Child |
| Membership | participation and authority within one Household; TARGET references Person | the human's identity across Households |
| Membership role | Owner/Member/Guest authority inside one Household | Parent/Child/Partner/Grandparent |
| Relationship | TARGET link between Persons, independent of co-residence | automatic permission or Household membership |
| Invitation | expiring offer to create/link access | permanent access grant |
| Assignment | responsibility attributed to Membership | ownership by an authentication record |
| Household context | requested HouseholdId authorized on the server | trusted global client-side active state |

`Account`, stable `Membership`, and their cardinality are accepted by
[ADR 0006](../adr/0006-separate-account-and-household-membership-identity.md),
and implemented for Household identity and the three lifecycle primitives.
Later linking, concurrent transitions and broader authorization remain target work.

## Accepted core model — TARGET

[ADR 0007](../adr/0007-person-as-stable-human-identity.md) extends the historical
Account/Membership separation in ADR 0006. It supersedes only the deferral of
Person and the direct Account-link target; it does not rewrite current code.

```text
AS-IS
Account -- optional direct link -- HouseholdMember -- Household
                                  MembershipId

TARGET (not implemented)
Account (credentials)
   | optional authentication link; exact mechanism OPEN
Person ---------------- Relationship ---------------- Person
   |
HouseholdMembership (participation; Owner / Member / Guest)
   |
Household

FUTURE (not designed for implementation)
Person (child) -- CareCircle -- CareCircleMembership -- Person (caregiver)
```

- Person is the human identity independent of login and any single Household.
  An adult may have an Account; a young child need not; a grandparent's Account
  is optional. Do not invent credentials to represent someone.
- Household remains an aggregate/participation context, not an entire family.
  Where product rules permit, Emma is one Person with memberships in both
  parents' Households. Do not duplicate her Person solely because they separate.
- MembershipId identifies participation, not the human. Preserve existing
  MembershipIds during the designed migration where participation continues.
  The exact Person link, uniqueness constraints and aggregate boundary need
  design; no new tables or framework are approved by this diagram.
- Relationships connect Persons independently of membership. Illustrations:
  Victor parent-of Emma, Anna parent-of Emma, Victor partner-of Anna, and Grethe
  grandparent-of Emma. These examples are not a finalized taxonomy.
- Child, parent, partner, grandparent and step-parent describe relationships or
  context. Do not create `Child : Person`, `Parent : Person` or similar subtypes.
- Family relationships never grant Household or care authority implicitly.
  Directionality, symmetry, effective dates, lifecycle and permissions remain
  open until a flow needs them.
- Account remains Infrastructure-owned ASP.NET Core Identity. The optional
  Account–Person persistence/linking mechanism and verification are OPEN.
  Do not add a Domain Account aggregate to mirror Identity.

### Incremental migration and unresolved design

Design the minimum Person state and creation/management authority, verified
Account linking, and handling of existing Account-linked and loginless members
before implementation. Loginless memberships contain insufficient data to infer
that two records are the same human; no matching by guesswork or automatic merge.
Preserve valid current Membership identities and authorization while moving in
bounded steps. Migration/backfill, constraints, compatibility and rollback
need an explicit plan based on actual data, not a destructive schema rewrite.

Person deletion/retention, Account deletion versus Person survival, and access
across contexts require explicit lifecycle decisions before affected writes.
The current Account-linked deletion policy below remains in force for AS-IS;
it is not an adopted cascade from Account to future Person/Relationship records.

### Future care and exploration

CareCircle/CareCircleMembership is an accepted **FUTURE concept**, distinct from
Household because caregivers need not live together. Parents, step-parents,
grandparents, babysitters and other trusted caregivers may eventually receive
scoped schedule, pickup, selected-information or temporary access. No detailed
medical, custody or child-privacy subsystem is designed now.

Playdates or temporary family coordination are **FUTURE EXPLORATION** only.
`HouseholdRelationship` is not accepted or scheduled; Person relationships or
temporary shared contexts may suffice. See the [product scope](../../product/PRODUCT-SCOPE.md).

### Adopted ownership and deletion rules

[DELETION-DESIGN.md](../../privacy/DELETION-DESIGN.md) is canonical. Membership
never implies ownership; there is no automatic Owner promotion. Owner requires
a real Account. Last Owner must explicitly TransferOwnership to a concrete
eligible Account-linked person or CloseHousehold; a continuing Household cannot
be ownerless. Destination acceptance remains OPEN.

LeaveHousehold, TransferOwnership and CloseHousehold are implemented, distinct
Testing-only lifecycles. DeleteAccount is NOT IMPLEMENTED in committed main;
its adopted design resolves all of an
Account's Household memberships, including loginless-person/shared-data
consequences. Null AccountId is not anonymization. The nullable Account FK to
AspNetUsers.Id is IMPLEMENTED (historical local verification: 2026-09-14) and rejects unresolved Identity
deletion without automatic Account-to-member cascade or SET NULL; see
[database guardrails](../../privacy/DELETION-DESIGN.md#database-guardrails).
Domain retains framework independence; lifecycle concurrency choices remain
OPEN. DeleteAccount membership fate is ADOPTED: delete all Account-linked memberships after ownership resolution,
before Identity deletion. AccountId remains nullable for separate loginless
creation/management.

Each persisted feature must define its scope and personal references, separate
leave/deletion/closure behavior, and surviving versus person-dependent records
using the [lifecycle checklist](../../privacy/DELETION-DESIGN.md#feature-lifecycle-checklist).
Creator identity does not imply Account ownership of shared data. Remove
unnecessary references from records that survive under explicit feature rules;
nulling alone is not verified anonymization. Concrete Task/Shopping/Event/
Routine lifecycle and attribution/history decisions remain OPEN.

## Aggregate reasoning

| Aggregate / concept | Status | Owns or protects |
|---|---|---|
| Household | CURRENT aggregate root, partial lifecycle | creation, small member set, duplicate link, transfer, Owner leave refusal and close; concurrent ownership protection remains |
| HouseholdMember | CURRENT entity inside Household | stable participation identity and optional Account link; link/unlink lifecycle remains |
| Person | Accepted TARGET concept, not implemented; aggregate boundary OPEN | stable human identity independent of credentials and participation |
| Relationship | Accepted TARGET concept, not implemented | links Persons independently of co-residence; detailed semantics OPEN |
| CareCircle / CareCircleMembership | FUTURE | care participation across Households; boundary/permissions not designed |
| Invitation | PROPOSED aggregate | token, target, expiry, revoke, single acceptance |
| HouseholdTask | PROPOSED aggregate | task state, assignment, due/completion/reopen |
| Routine | PROPOSED aggregate | recurrence definition and occurrence semantics |
| ShoppingList | PROPOSED aggregate | list/item state and active-list rule |
| HouseholdEvent | PROPOSED aggregate | internal event lifecycle/time rules |
| Today | NOT AN AGGREGATE | side-effect-free read composition |
| Account | IDENTITY BOUNDARY | credentials/authentication lifecycle outside Households Domain |

Stable `MembershipId` alone does not justify a separate Membership aggregate.
Revisit only when independent lifecycle, large membership sets, capabilities,
or contention make the Household consistency boundary harmful.

Detailed reasoning is preserved in the
[aggregate pressure test](../research/AGGREGATE-PRESSURE-TEST.md).

## Value objects

Current code has no explicit value-object type. Candidates are introduced only
when their behavior pays for the type:

- a HouseholdName value object only if repeated name behavior justifies it; the
  current primitive string is bounded before durable mapping;
- RecurrencePattern and civil-time semantics before recurring tasks;
- provider/external identifiers only inside Calendar Integration;
- strongly typed IDs only if repeated wrong-ID defects justify them.

## Domain services and domain events

- **Domain services:** not yet justified. Add one only for a stateless business
  rule spanning concepts that fits no entity/value object.
- **Domain events:** not yet justified. Add only when one committed fact has
  multiple independent reactions and direct orchestration becomes coupled.
- **Integration events, outbox, and broker:** separate decisions with separate
  triggers; use the [evolution policy](TARGET-ARCHITECTURE.md#evolution-policy-now-next-later-if-needed).
  Domain events alone imply none of them.

## Required invariant evidence

Before a concept is called implemented, prove its invariant at the appropriate
layers:

| Invariant | Domain | Database/concurrency | End-to-end |
|---|---|---|---|
| valid Household name | unit behavior | length/check mapping | safe 400 contract |
| initial and last Owner | aggregate behavior | version/conditional race proof | authorized conflict response |
| scoped Account link | link behavior | current unfiltered (HouseholdId, AccountId) uniqueness; future linking race proof | verified actor/link flow |
| Membership identity and lifecycle | identity behavior and explicit deletion rules | key/FK/retention mapping | transfer preserves identity; leave deletes membership; future link/unlink and attribution rules |
| recurrence uniqueness | value/aggregate behavior | unique occurrence key | retry and DST cases |
| resource authorization | policy/use case | focused access query | IDOR/BOLA matrix |

## Deferred concepts

CareCircle implementation, granular capability ACLs, arbitrary social graphs,
meals, expenses, maintenance, location, rewards, general RRULE, calendar sync,
push infrastructure, and realtime transport remain deferred until explicit
product and lifecycle evidence exists.
