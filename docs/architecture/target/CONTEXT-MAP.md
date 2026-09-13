# HomePlatform Context Map

Status: **Authoritative context description**  
Last reviewed: **2026-09-13**

## Current implemented state

The codebase currently has layered projects and one implemented Households
slice for creating and persisting a Household with its initial Owner
Membership. It does not yet have a complete Household lifecycle or fully
enforced bounded contexts. Roleless Identity persistence and implemented Account
Registration exist (`e2fca98`; historical registration baseline: 72/72 tests).
Bearer sign-in is implemented in `4180096`; anonymous Refresh is implemented,
validates expiry/security stamp, and issues new access/refresh tokens. Permanent
real-bearer PostgreSQL tests prove renewed access and the persisted Household
AccountId. Confirmation/recovery/revocation and release gates remain incomplete,
as are Tasks & Routines, Shopping, Events, Today, Notifications,
and Calendar Integration. Folder names alone are not bounded-context evidence.

## Candidate target boundaries

The following map proposes language and ownership; it does not pre-approve one
bounded context for every feature area. Account/Membership
cardinality follows accepted
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
| Households | **CURRENT, partial** | Household creation, Membership identity/roles, loginless members, scoped Account-link uniqueness | credentials, tasks, lists, events |
| Identity & Access | **PARTIAL supporting capability** | Account credentials, authentication/session/recovery lifecycle | household role or resource access |
| Households target | **PARTIAL** | stable Membership implemented; invitations, last-Owner lifecycle, verified linking, and Household authorization remain | passwords, unrelated feature state |
| Tasks & Routines | **PROPOSED** | task lifecycle, assignment, recurrence and occurrence identity | membership source of truth |
| Shopping | **PROPOSED** | lists/items and their transitions | catalog, recipes, membership |
| Events | **PROPOSED** | internal household event lifecycle and time rules | external provider tokens/cursors |
| Today / Agenda | **PROPOSED read composition** | authorized query/DTO composition | write aggregate or cross-context transaction |
| Notifications / Delivery | **DEFERRED supporting module** | preferences/delivery only after a real unattended reminder | feature business rules |
| Calendar Integration | **DEFERRED supporting module** | provider mapping/cursors/conflicts after one provider is approved | internal event language |
| People / Profile | **DEFERRED** | nothing until a cross-household profile lifecycle is proven | a pre-schema shortcut for Membership |

## Responsibilities

### Identity & Access

Language: Account, credential, authentication subject, confirmation, session,
refresh, lockout, recovery, revocation.

ASP.NET Core Identity remains Infrastructure-owned. Application uses
ICurrentAccount for actor identity, IAccountRegistration for registration,
IAccountAuthentication for sign-in, and IAccountRefresh for renewal. Each Account
operation keeps its command, handler, validator, port, result, and errors in
Accounts/Register, Accounts/SignIn, or Accounts/Refresh. No Identity framework
types cross those ports. No household roles live in Identity.

### Households

Language: Household, Membership, Owner, Member, Guest, Invitation, join, leave,
remove, transfer, link Account.

Households owns stable `MembershipId`, optional verified `AccountId`, scoped
duplicate protection, last-Owner consistency, and resource-authorization facts.
The first three are represented in the current model; verified link lifecycle,
last-Owner transitions, and resource authorization remain. Family relationships
never grant authority implicitly.

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

## When a module becomes a bounded context

Promote a candidate only when it has a coherent ubiquitous language, its own
invariants and model, an independent lifecycle, an explicit ownership boundary,
and distinct reasons to change. Document where the same term has a different
meaning and how contracts translate it. A feature folder, table, or endpoint
alone meets none of these criteria.

Currently Households is the one small product-domain model; Identity is a
separate credential-owning supporting capability. This is useful separation,
but does not establish several mature implemented bounded contexts. Tasks,
Shopping, and internal Calendar may remain modules in one coordination context
until their models diverge. Today is a query surface, not a bounded context by
default. Notifications, finances, and home automation are future candidates,
not mandatory contexts. Keep the modular monolith even if contexts emerge;
service extraction requires an additional deployment/scaling/ownership need.

## Later candidates

Meals, home maintenance, expenses, care logistics, cross-household connections,
location/safety, and rewards/allowance remain discovery candidates. They must
not become fields on Household or Task merely because competitors contain them.

## Evidence

This map consolidates the decision-relevant
[competitor DDD summary](../research/COMPETITOR-IMPACT-ON-DDD.md),
[aggregate pressure test](../research/AGGREGATE-PRESSURE-TEST.md), and archived
[bounded-context proposal](../../research/archive/competitor-audit-2026-08-29/PROPOSED-BOUNDED-CONTEXTS.md).
