# HomePlatform Context Map

Status: **Authoritative context description**  
Last reviewed: **2026-09-20**; AS-IS baseline `main@7162c35`

## Current implemented state

The codebase has four layer projects, one small Household aggregate and
Infrastructure-owned roleless Identity. Registration, bearer sign-in and refresh
are implemented. CreateHousehold, TransferOwnership, LeaveHousehold and
CloseHousehold are implemented with membership/Owner checks and Testing-only
HTTP endpoints. The nullable Account FK guards references and unresolved deletion.

DeleteAccount is absent from this committed snapshot (uncommitted work exists).
Current-Account validity, optimistic concurrency, invitations/linking and broader
security/release gates remain incomplete. Tasks & Routines, Shopping, Events,
Today, Notifications and Calendar Integration are not implemented.
[NEXT-STEPS](../roadmap/NEXT-STEPS.md) records verification limits; this is source
inspection, not current test or deployment proof. Folder names alone are not
bounded-context evidence.

## Accepted concepts and candidate target boundaries

[ADR 0007](../adr/0007-person-as-stable-human-identity.md) accepts Person and
Relationship concepts. This map describes ownership/language, not a requirement
for one bounded context, project, database or service per concept.

```text
Identity & Access: Account / credentials (Infrastructure)
                         |
               optional link; design OPEN
                         v
People: Person -------- Relationship -------- Person   [TARGET]
          |
Households: HouseholdMembership --> Household          [TARGET evolution]
          |
          +--> Tasks / Shopping / Events --> Today     [candidate flows]

Person (child) --> CareCircle <-- CareCircleMembership <-- Person (caregiver)
                         [FUTURE only]
```

**AS-IS:** Account references HouseholdMember directly. **TARGET:** credentials,
human identity, participation and family/social relationships have separate
meanings. Co-residence does not imply family relationship; a Person may belong
to multiple Households where rules permit. Context boundaries and contracts
must be refined through real flows, not by pre-creating modules.

## Status by boundary

| Boundary | Status | Owns | Does not own |
|---|---|---|---|
| Households | **CURRENT, partial** | creation, transfer/leave/close, Membership identity/roles, loginless members, scoped Account-link uniqueness | credentials, tasks, lists, events |
| Identity & Access | **PARTIAL supporting capability** | Account credentials, authentication/session/recovery lifecycle | household role or resource access |
| Households target | **TARGET evolution** | Person participation through HouseholdMembership; retain contextual authority | human identity, credentials, relationship source of truth |
| Tasks & Routines | **PROPOSED** | task lifecycle, assignment, recurrence and occurrence identity | membership source of truth |
| Shopping | **PROPOSED** | lists/items and their transitions | catalog, recipes, membership |
| Events | **PROPOSED** | internal household event lifecycle and time rules | external provider tokens/cursors |
| Today / Agenda | **PROPOSED read composition** | authorized query/DTO composition | write aggregate or cross-context transaction |
| Notifications / Delivery | **DEFERRED supporting module** | preferences/delivery only after a real unattended reminder | feature business rules |
| Calendar Integration | **DEFERRED supporting module** | provider mapping/cursors/conflicts after one provider is approved | internal event language |
| People / Relationships | **Accepted TARGET concepts; boundary OPEN** | stable Person identity and relationships independent of co-residence | credentials or implicit Household permissions |
| CareCircle / CareCircleMembership | **FUTURE** | child-centered care participation across Households | identity of a Household or an immediate implementation commitment |

## Responsibilities

### Identity & Access

Language: Account, credential, authentication subject, confirmation, session,
refresh, lockout, recovery, revocation, DeleteAccount.

ASP.NET Core Identity remains Infrastructure-owned. Application uses
ICurrentAccount for actor identity, IAccountRegistration for registration,
IAccountAuthentication for sign-in, and IAccountRefresh for renewal. Each Account
operation keeps its command, handler, validator, port, result, and errors in
Accounts/Register, Accounts/SignIn, or Accounts/Refresh. No Identity framework
types cross those ports. No household roles live in Identity.

### People and Relationships — TARGET

Person is the human, optionally linked to an Account. Children, parents,
partners, grandparents and step-parents are Persons in relationships/context,
not entity subtypes. Relationship directionality, symmetry, taxonomy, lifecycle,
effective dates and permissions are open. Person management, verified linking,
matching and deletion must be designed before affected persistence is added.
No dedicated People project or graph database is implied.

### Households

Language: Household, Membership, Owner, Member, Guest, Invitation, join, leave,
remove, link Account, LeaveHousehold, TransferOwnership, CloseHousehold.

**AS-IS:** Households owns stable `MembershipId`, optional `AccountId`, scoped
duplicate protection, last-Owner consistency, and resource-authorization facts.
Identity/link integrity and sequential lifecycle checks are represented in the
current model; verified linking, concurrent ownership safety and broader
authorization remain. Family relationships
never grant authority implicitly. **TARGET:** HouseholdMembership references
the Person participating in that Household; credentials are reached through a
separately designed Account–Person link. Multiple memberships do not duplicate
the human identity or grant access between Households.

### Account and Household lifecycle coordination

**ADOPTED:** [DELETION-DESIGN.md](../../privacy/DELETION-DESIGN.md) separates
Account identity data, the departing person's data/memberships, other people's
data and shared Household/domain data. DeleteAccount coordinates all Household
memberships through explicit lifecycle before deleting Identity. Households
owns implemented LeaveHousehold, explicit TransferOwnership and CloseHousehold
rules. DeleteAccount coordination is not implemented in committed main.

Membership never implies ownership. A continuing Household retains an
Account-linked Owner; last Owner explicitly transfers to a concrete eligible
Account-linked person or closes. Loginless members cannot become Owner and may
still represent real people. Owner is a Household role, not ownership of those
people's personal data. Shared record ownership is distinct from Account
ownership. DeleteAccount membership fate is ADOPTED: explicitly delete all
memberships linked to the AccountId after ownership resolution, then delete
Identity/ApplicationUser and Account-owned data. Do not convert those
memberships to loginless. Nullable AccountId remains for separate loginless
creation/management. Concrete future attribution/history remains OPEN.

Each feature owns its persisted record lifecycle, using the
[canonical checklist](../../privacy/DELETION-DESIGN.md#feature-lifecycle-checklist).
Account/Membership deletion is not a cascade policy for every shared record.
Feature rules decide continued purpose and survival; remove unnecessary person
references and define missing-attribution UI. Do not invent a generic usage
heuristic, replacement identity or anonymization claim from a null reference.

Infrastructure now enforces the guarding nullable AccountId FK to
AspNetUsers.Id; this physical constraint does not move Identity types into
Domain. Current Household membership authorization remains required;
immediate current-Account validity is deferred security hardening, with the
access-token expiry window recorded in the [security roadmap](../roadmap/SECURITY-ROADMAP.md).
The Person target requires a new lifecycle decision before replacing current
Account-linked deletion rules; Account deletion does not yet define Person fate.
Reuse concrete deletion operations for
appropriate privacy requests without introducing a generic privacy context or
GdprService/PrivacyService.

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

The proposed Today is a side-effect-free Application read composition over authorized task,
event, and shopping projections. It is not an aggregate or default table.

## Integration relationships

| Upstream | Downstream | Relationship/contract |
|---|---|---|
| Identity & Access | People (TARGET) | optional verified Account–Person link; exact mechanism OPEN |
| People | Households (TARGET) | Person identity for participation; explicit authorized contract, no automatic cross-Household access |
| Identity & Access | Households (AS-IS) | trusted AccountId and current direct membership lookup |
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

CareCircle/CareCircleMembership remain future care concepts, not the next slice.
Playdates and temporary trusted sharing remain exploration; no
HouseholdRelationship entity, schema or roadmap item is accepted.

Meals, home maintenance, expenses, care logistics,
location/safety, and rewards/allowance remain discovery candidates. They must
not become fields on Household or Task merely because competitors contain them.

## Evidence

This map consolidates the decision-relevant
[competitor DDD summary](../research/COMPETITOR-IMPACT-ON-DDD.md),
[aggregate pressure test](../research/AGGREGATE-PRESSURE-TEST.md), and archived
[bounded-context proposal](../../research/archive/competitor-audit-2026-08-29/PROPOSED-BOUNDED-CONTEXTS.md).
