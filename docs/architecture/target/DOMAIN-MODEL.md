# HomePlatform Domain Model

Status: **Authoritative current-versus-target DDD description**  
Last reviewed: **2026-08-29**

## Current implemented state

The current Domain is small and still evolving:

| Concept | Current shape | Current status |
|---|---|---|
| Household | Guid identity, trimmed nonblank Name, UTC timestamps, private member list | aggregate-root candidate; edited constructor requires Owner AccountId and creates Membership |
| HouseholdMember | generated `MembershipId`, optional `AccountId`, HouseholdRole | uncommitted prototype of proposed ADR 0006; no link/lifecycle behavior |
| HouseholdRole | Owner, Member, Guest | authorization vocabulary present; enforcement incomplete |
| User | Guid, username, email | ownership is unclear; must not duplicate ASP.NET Core Identity credentials |
| Result | success/failure primitive | small Domain helper, not an HTTP contract |

The inspected working tree is internally inconsistent: Household now accepts an
Owner AccountId and creates a Membership, while the current handler and tests
still call older constructors and member APIs. No active EF mapping or migration
makes this a durable database model. The source prototype does not accept ADR
0006 and must not be presented as a finished aggregate design.

## Implemented invariants

Source inspection shows these early rules:

- Household Name cannot be blank and is trimmed.
- the edited Household constructor rejects an empty Owner AccountId and creates
  one Owner Membership;
- HouseholdMember generates MembershipId and rejects empty AccountId/undefined
  roles;
- `AddMember` can create a loginless Membership and rejects duplicate linked
  AccountId within one Household instance;
- member addition updates `UpdatedAt`.

Not yet proven or implemented consistently:

- bounded Name length;
- collection exposure that cannot be downcast/mutated;
- ADR approval and stable Membership identity across persistence/lifecycle;
- later verified Account linking and unlinking;
- last-Owner behavior across every leave/remove/demote race;
- persistence constraints, concurrency tokens, or PostgreSQL round-trip;
- resource authorization and trusted actor boundary.

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

`Account`, stable `Membership`, and their cardinality remain proposed by
[ADR 0006](../adr/0006-separate-account-and-household-membership-identity.md),
not implemented fact.

## Proposed core model

If ADR 0006 is accepted:

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
- one active linked Membership per `(HouseholdId, AccountId)` is the likely
  scoped uniqueness rule;
- loginless Membership can later link an Account without changing
  `MembershipId`, assignments, or history;
- Account linking/unlinking is an explicit verified use case;
- a global Person/Profile is not introduced without independent behavior and
  cross-household ownership rules.

## Aggregate reasoning

| Aggregate / concept | Status | Owns or protects |
|---|---|---|
| Household | CURRENT candidate, target retained | small member set, role transitions, duplicate link, last Owner |
| HouseholdMember | PROPOSED entity inside Household | stable participation identity and link state; no repository yet |
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

- bounded HouseholdName before durable mapping;
- RecurrencePattern and civil-time semantics before recurring tasks;
- provider/external identifiers only inside Calendar Integration;
- strongly typed IDs only if repeated wrong-ID defects justify them.

## Domain services and domain events

- **Domain services:** not yet justified. Add one only for a stateless business
  rule spanning concepts that fits no entity/value object.
- **Domain events:** not yet justified. Add only when one committed fact has
  multiple independent reactions and direct orchestration becomes coupled.
- **Outbox/broker:** not justified by DDD alone. Require durable post-commit work
  and prove the failure/retry need first.

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
