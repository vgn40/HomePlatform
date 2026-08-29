# HomePlatform Context Map

Status: **Authoritative context description**  
Last reviewed: **2026-08-29**

## Current implemented state

The codebase currently has layered projects and a nascent Household model, not
fully enforced bounded contexts. There is no implemented Identity & Access,
Tasks & Routines, Shopping, Events, Today, Notifications, or Calendar
Integration context. Folder names alone are not bounded-context evidence.

## Proposed initial map

The following map is target language and ownership. Account/Membership
cardinality depends on proposed
[ADR 0006](../adr/0006-separate-account-and-household-membership-identity.md).

```text
                         +-------------------+
                         | Identity & Access |
                         | Account / session |
                         +---------+---------+
                                   | trusted AccountId
                                   v
                         +---------+---------+
                         |    Households     |
                         | Membership / ACL  |
                         +----+----+----+-----+
                              |    |    |
                              v    v    v
                    Tasks & Routines  Shopping  Events
                              \       |       /
                               +------v------+
                               | Today/Agenda|
                               | read model  |
                               +-------------+

Application orchestration -> email, push, and calendar adapters when triggered
```

## Status by boundary

| Boundary | Status | Owns | Does not own |
|---|---|---|---|
| Households | **CURRENT, early/incomplete** | Household name, current member/role prototype | credentials, tasks, lists, events |
| Identity & Access | **PROPOSED** | Account credentials, authentication/session/recovery lifecycle | household role or resource access |
| Households target | **PROPOSED** | stable Membership, invitations, Owner invariant, Household authorization facts | passwords, unrelated feature state |
| Tasks & Routines | **PROPOSED** | task lifecycle, assignment, recurrence and occurrence identity | membership source of truth |
| Shopping | **PROPOSED** | lists/items and their transitions | catalog, recipes, membership |
| Events | **PROPOSED** | internal household event lifecycle and time rules | external provider tokens/cursors |
| Today / Agenda | **PROPOSED read composition** | authorized query/DTO composition | write aggregate or cross-context transaction |
| Notifications / Delivery | **DEFERRED supporting module** | preferences/delivery only after a real unattended reminder | feature business rules |
| Calendar Integration | **DEFERRED supporting module** | provider mapping/cursors/conflicts after one provider is approved | internal event language |
| People / Profile | **DEFERRED** | nothing until a cross-household profile lifecycle is proven | a pre-schema shortcut for Membership |

## Proposed responsibilities

### Identity & Access

Language: Account, credential, authentication subject, confirmation, session,
refresh, lockout, recovery, revocation.

ASP.NET Core Identity remains Infrastructure-owned. Application receives only a
trusted Account identity through a narrow port. No household roles live in
Identity.

### Households

Language: Household, Membership, Owner, Member, Guest, Invitation, join, leave,
remove, transfer, link Account.

If ADR 0006 is accepted, Households owns stable `MembershipId`, optional
verified `AccountId`, scoped duplicate protection, last-Owner consistency, and
resource-authorization facts. Family relationships never grant authority
implicitly.

### Tasks & Routines

Language: task, assignee Membership, due time, complete, reopen, Routine,
RecurrencePattern, occurrence, skip, series.

Assignments reference Membership identity. Recurrence must define timezone,
DST, missed-occurrence, edit-one/edit-series, and idempotent occurrence
semantics before persistence.

### Shopping

Language: ShoppingList, ShoppingItem, add, check, uncheck, remove, archive,
active list. It owns list/item consistency, not recipes or product catalog.

### Events

Language: HouseholdEvent, start/end, local timezone, audience, cancel. External
provider identifiers and sync state stay behind a later anti-corruption layer.

### Today / Agenda

Today is a side-effect-free Application read composition over authorized task,
event, and shopping projections. It is not an aggregate or default table.

## Integration relationships

| Upstream | Downstream | Relationship/contract |
|---|---|---|
| Identity & Access | Households | trusted AccountId and explicit account-link verification |
| Households | Tasks/Shopping/Events | stable Membership/access query contract; no Domain/EF entity leakage |
| Tasks/Shopping/Events | Today | stable read DTO/projection; no cross-context write |
| Feature modules | Notifications | explicit delivery intent only when a concrete reminder exists |
| Calendar provider | Calendar Integration | anti-corruption mapping of provider IDs, cursors, and tombstones |
| Calendar Integration | Events/Today | explicit mapping/query; provider objects stay outside Domain |

## Boundary rules

1. Contexts do not imply projects, services, schemas, or network calls.
2. Reference other contexts by IDs and explicit Application contracts.
3. Do not place tasks, lists, events, or Today inside the Household aggregate.
4. Do not share Domain/EF/Identity entities across boundaries.
5. One use case normally owns one commit.
6. Introduce supporting contexts only when their independent language,
   lifecycle, and invariants are demonstrated.

## Later candidates

Meals, home maintenance, expenses, care logistics, cross-household connections,
location/safety, and rewards/allowance remain discovery candidates. They must
not become fields on Household or Task merely because competitors contain them.

## Evidence

This map consolidates the decision-relevant
[competitor DDD summary](../research/COMPETITOR-IMPACT-ON-DDD.md),
[aggregate pressure test](../research/AGGREGATE-PRESSURE-TEST.md), and archived
[bounded-context proposal](../../research/archive/competitor-audit-2026-08-29/PROPOSED-BOUNDED-CONTEXTS.md).
