# HomePlatform Domain Model

Status: **Authoritative current-versus-target DDD description**  
Last reviewed: **2026-09-13**

## Current implemented state

The current Domain is small and still evolving. Its first Household slice is
implemented and persisted, while membership lifecycle and authorization remain:

| Concept | Current shape | Current status |
|---|---|---|
| Household | Guid identity, trimmed Name bounded to 100 characters, UTC timestamps, private member list | aggregate root for the implemented CreateHousehold slice; creates one Account-linked Owner Membership |
| HouseholdMember | generated `MembershipId`, optional `AccountId`, HouseholdRole | implemented entity inside Household; persisted with scoped uniqueness; no link/unlink lifecycle behavior |
| HouseholdRole | Owner, Member, Guest enum | vocabulary, not a value object or domain service; resource enforcement and role transitions incomplete |
| Account | Guid reference only in Household Domain | credentials belong to Infrastructure ApplicationUser/Identity; the former Domain User has been removed |
| Result | success/failure primitive | ordinary result class; neither entity, value object, nor domain service |

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
- member addition updates `UpdatedAt`;
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
- production resource authorization. The trusted actor boundary is proven only
  in the Testing-only CreateHousehold route.

The private constructors are EF materialization hooks, not public ways to
construct invalid entities. Mappings preserve encapsulation, but EF and direct
database writes can bypass public constructor checks. The database has no role,
Owner-presence, nonblank-name, or Account-existence checks. There is no public
child mutation API; future membership writes must remain aggregate-owned.

Current maturity is a domain model with dependency inversion and one explicit
aggregate (Level 2 overall, early Level 3 in Household). More contexts, events,
and distribution would not by themselves improve model quality.

## Ubiquitous language

| Term | Meaning | Must not mean |
|---|---|---|
| Account | credential-bearing authentication identity | a participant in every Household |
| Household | protected collaboration boundary | global family graph or authentication tenant |
| Membership | one participant's identity and lifecycle inside one Household | Account or family relationship |
| Membership role | Owner/Member/Guest authority inside one Household | Parent/Child/Partner/Grandparent |
| Relationship | optional descriptive family/care meaning | automatic permission |
| Invitation | expiring offer to create/link access | permanent access grant |
| Assignment | responsibility attributed to Membership | ownership by an authentication record |
| Household context | requested HouseholdId authorized on the server | trusted global client-side active state |

`Account`, stable `Membership`, and their cardinality are accepted by
[ADR 0006](../adr/0006-separate-account-and-household-membership-identity.md),
and implemented for the first Household slice. Later linking, lifecycle, and
authorization behavior remains target work.

## Accepted core model

```text
Identity & Access
  Account
    AccountId
        |
        | optional verified link
        v
Households
  Household (aggregate root)
    HouseholdMember (entity)
      MembershipId
      AccountId?
      Role: Owner | Member | Guest
      Lifecycle: only when concrete use cases define it
```

- one Account may link to Memberships in several Households;
- one non-null Account link per `(HouseholdId, AccountId)` is enforced; the
  current schema has no active/inactive state or filtered index;
- approved link/unlink behavior must preserve MembershipId while retaining the
  Membership; that workflow is not implemented. DeleteAccount explicitly
  deletes Account-linked memberships after ownership resolution, without
  loginless conversion. Concrete future attribution/history remains OPEN;
  identity stability does not require indefinite data retention;
- a global Person/Profile is not introduced without independent behavior and
  cross-household ownership rules.

### Adopted ownership and deletion rules

[DELETION-DESIGN.md](../../privacy/DELETION-DESIGN.md) is canonical. Membership
never implies ownership; there is no automatic Owner promotion. Owner requires
a real Account. Last Owner must explicitly TransferOwnership to a concrete
eligible Account-linked person or CloseHousehold; a continuing Household cannot
be ownerless. Destination acceptance remains OPEN.

DeleteAccount, LeaveHousehold, TransferOwnership and CloseHousehold are
different, NOT YET IMPLEMENTED lifecycles. DeleteAccount resolves all of an
Account's Household memberships, including loginless-person/shared-data
consequences. Null AccountId is not anonymization. A future guarding nullable
FK to AspNetUsers.Id must reject unresolved Identity deletion, with no automatic
Account-to-member cascade or SET NULL. Domain retains framework independence;
precise EF/concurrency choices remain OPEN. DeleteAccount membership fate is
ADOPTED: delete all Account-linked memberships after ownership resolution,
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
| Household | CURRENT aggregate root, partial lifecycle | creation, small member set, duplicate link; role transitions and last Owner remain |
| HouseholdMember | CURRENT entity inside Household | stable participation identity and optional Account link; link/unlink lifecycle remains |
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
| scoped Account link | link behavior | filtered uniqueness/concurrency | verified actor/link flow |
| stable Membership history | identity behavior | key/FK/retention mapping | link/unlink/leave flow |
| recurrence uniqueness | value/aggregate behavior | unique occurrence key | retry and DST cases |
| resource authorization | policy/use case | focused access query | IDOR/BOLA matrix |

## Deferred concepts

Global Person/Profile, granular capability ACLs, cross-household social graph,
meals, expenses, maintenance, location, rewards, general RRULE, calendar sync,
push infrastructure, and realtime transport remain deferred until explicit
product and lifecycle evidence exists.
