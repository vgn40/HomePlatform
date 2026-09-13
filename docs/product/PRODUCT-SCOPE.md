# HomePlatform Product Scope

Status: **Authoritative product scope for the six-month beta**  
Last reviewed: **2026-09-13**

## Product thesis

HomePlatform should reduce everyday household coordination friction through a
trusted shared space for responsibility, shopping, events, and today's agenda.
The differentiation is not the largest feature list. It is reliable Household
identity and authority, clear ownership of work, low-friction collaboration,
and honest lifecycle behavior.

## Current implemented state

The repository has a complete Testing-only CreateHousehold path, roleless
Identity persistence, and implemented Account Registration (committed in
`e2fca98`; verified solution baseline: 72/72 tests). There is no complete
user-facing authenticated Household experience or frontend yet. Registration
alone does not establish sign-in, verified identity, or Household authorization.

## Six-month beta scope

| Capability | Target | Product constraint |
|---|---|---|
| Account lifecycle | register, verify, sign in, recover, revoke, DeleteAccount | explicit multi-Household resolution; immediate protected-request denial after deletion commit |
| Household | create, rename, list, CloseHousehold | protected collaboration boundary; closure is explicit |
| Membership | Owner/Member/Guest, invite, join, leave/remove/transfer | role is authority, not family relationship |
| Loginless participant | model support implemented under accepted ADR 0006 | identity flexibility now; specialized child UI later |
| Tasks | create, assign, complete/reopen, history | assignment by Membership identity under ADR 0006 |
| Routines | deliberately limited recurrence | timezone/DST/idempotency before breadth |
| Shopping | shared list and item transitions | one active list initially, not permanent contract |
| Today | authorized daily read composition | no write aggregate or GET side effects |
| Events | basic internal events if capacity remains | no external provider objects/sync |
| Trust/operations | export/deletion, backup/restore, observability | required before public beta claim |

## Structural product decisions

- Account authentication and Household participation are different lifecycles;
  their separation is accepted in
  [ADR 0006](../architecture/adr/0006-separate-account-and-household-membership-identity.md).
- One Account can have memberships in several Households; DeleteAccount must
  resolve all of them.
- Owner/Member/Guest express authorization. Parent/Child/Partner/Grandparent
  describe relationships and do not automatically grant access.
- Today is a read surface over several modules, not a new write model.
- Recurrence, reminders, and collaboration quality matter more than a long
  checklist of shallow features.

## Adopted deletion and ownership lifecycle

[DELETION-DESIGN.md](../privacy/DELETION-DESIGN.md) is canonical. DeleteAccount,
LeaveHousehold, TransferOwnership and CloseHousehold are distinct, adopted
operations and are NOT YET IMPLEMENTED. A continuing Household needs an Owner
linked to a real Account. Membership and family relationships never cause
automatic Owner promotion. The last Owner must explicitly transfer to a concrete
eligible Account-linked person or explicitly close the Household.

Deletion must distinguish the departing person's data/memberships, other
people's data (including loginless children), and shared Household data. Owner
is an authority role, not ownership of another person's personal data. A
Household used only by the departing Account can be explicitly closed with
clear information that its data will be deleted; no grace period is adopted.

**ADOPTED:** after ownership rules are resolved, DeleteAccount explicitly
deletes every HouseholdMember linked to the AccountId across all Households,
then Identity/ApplicationUser and Account-owned data. Memberships are never
silently converted to loginless; AccountId remains nullable because loginless
creation/management is a separate supported flow.

Every persisted feature must define record scope, personal references,
LeaveHousehold/DeleteAccount/CloseHousehold, surviving versus person-dependent
records, missing-attribution UI and export/retention/privacy consequences. Use
the [feature lifecycle checklist](../privacy/DELETION-DESIGN.md#feature-lifecycle-checklist).
Creator deletion does not automatically delete legitimate shared records;
explicit feature rules decide their lifecycle. Remove unnecessary person
references and do not retain personal data “just in case” or claim nulling
alone anonymizes it.

Destination acceptance, concrete future feature lifecycles/attribution/history,
reauthentication UX, child-account policy, retention and backup periods remain
OPEN. The canonical design lists all open decisions and required release gates.

## Deferred product scope

Defer until interviews/pilot evidence and the core beta gates justify it:

- caregiver/child workflows beyond the minimum identity model;
- cross-Household sharing or discovery;
- multiple shopping lists/stores and meal planning;
- expenses, home maintenance, care logistics, rewards/allowance;
- external calendar synchronization;
- AI ingestion/automation;
- location/safety tracking;
- wall-display/kiosk hardware modes;
- push/realtime infrastructure.

These areas have distinct language, privacy, reliability, or operational cost.
They must not be added as fields on Household or Task.

## Explicit non-goals

- competing on feature count;
- turning family relationship labels into permissions;
- forcing credentials onto every represented Household participant;
- inventing a global Person graph before a real cross-Household lifecycle;
- using realtime, AI, or hardware to hide weak core workflows;
- weakening authorization, concurrency, migration, recovery, or data-lifecycle
  gates to ship more surface area.

## Beta success evidence

- a new Account can securely create and activate a Household;
- invited participants reach a first shared action with low friction;
- assignments/history follow explicit feature lifecycle rules when memberships
  change or are deleted, including removal of unnecessary personal references;
- tasks/routines and shopping behave predictably under retries/concurrency;
- Today is useful without mutating data;
- users can understand export/deletion/shared-data consequences;
- the product is recoverable, observable, and explicit about limitations.

The engineering order and exit gates live in the
[masterplan](../architecture/roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md).
