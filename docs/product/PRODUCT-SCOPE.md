# HomePlatform Product Scope

Status: **Product direction and candidate scope; execution owned by NEXT-STEPS**
Last reviewed: **2026-09-20**; AS-IS baseline `main@7162c35` plus separately identified working-tree work

## Product thesis

HomePlatform helps people coordinate shared life across households, relationships
and changing family structures.

The long-term model should serve couples living together, households without
children, families with children, blended families, separated parents,
grandparents and other caregivers. A change in family structure should not
require a different product or a duplicate identity for the same child.
These are goals for incremental evolution, not features available today.

## Current implemented state

Registration, bearer sign-in, Refresh and the nullable Account FK are
implemented. CreateHousehold, TransferOwnership, LeaveHousehold and
CloseHousehold exist with Testing-only HTTP routes and concrete membership/Owner
checks. Current-Account validity and optimistic concurrency remain incomplete.
DeleteAccount is not implemented in committed main; separate uncommitted work
exists. There is no frontend or complete user-facing Household experience.
This is source status, not fresh test, hosted CI or deployment evidence.

## Product/domain direction — TARGET

| Concept | Meaning | Status |
|---|---|---|
| Account | authentication and credentials, owned by Infrastructure Identity | AS-IS |
| Person | stable human identity; may optionally have an Account | Accepted TARGET; not implemented |
| HouseholdMembership | a Person's participation and authority role in one Household | TARGET evolution of current Account-linked HouseholdMember |
| Relationship | relationship between Persons independent of Household membership | Accepted TARGET; implement only for a concrete flow |
| CareCircle / CareCircleMembership | people caring for a child across Household boundaries | FUTURE; not an immediate implementation task |

An adult user has a Person and Account; a young child has a Person without an
Account; a grandparent may have either arrangement. Child, parent, partner,
grandparent and step-parent describe relationships/context, not Person subtypes.
Owner/Member/Guest are Household authority roles and do not follow from family
relationships.

Household is a participation context, not the identity of an entire family.
For example, Emma remains one Person with separate memberships in her mother's
and father's Households after separation, where product rules permit this.
There is no automatic sharing or permission grant between those Households.

[ADR 0007](../architecture/adr/0007-person-as-stable-human-identity.md) records
the decision. The exact Account–Person link, Person data/management rules and
migration remain open. [ADR 0006](../architecture/adr/0006-separate-account-and-household-membership-identity.md)
remains the historical Account/Membership separation; its deferral of Person
has been superseded.

## Incremental product evolution

The next work designs the smallest path from Account-linked memberships to
Person, implements a minimal foundation, evolves membership, and proves value
with a concrete flow independent of login. A candidate is representing and
showing a loginless participant as one Person; detailed fields and permissions
must be chosen before implementation. This is not approval for a child/custody
subsystem or automatic identity matching.

Relationships follow a real product flow; care capabilities follow later needs.
Tasks, Routines, Shopping, Today and Events remain possible product slices,
not a delivery commitment. Today remains read composition, not a write aggregate.
Only [NEXT-STEPS](../architecture/roadmap/NEXT-STEPS.md) sets execution order.

HomePlatform remains a .NET/backend portfolio. Its architectural story is the
separation of credentials, people, participation and relationships, supported
by working vertical slices. Product/domain development proceeds while known
technical risks remain explicit in the
[debt register](../architecture/roadmap/TECHNICAL-DEBT-REGISTER.md).
Pull hardening forward for actual risk, a feature dependency or release exposure;
real-user privacy/security and operational gates still apply.

## Adopted deletion and ownership lifecycle

[DELETION-DESIGN.md](../privacy/DELETION-DESIGN.md) is canonical. DeleteAccount,
LeaveHousehold, TransferOwnership and CloseHousehold are distinct, adopted
operations. The three Household operations are implemented with Testing-only
routes; DeleteAccount remains absent from committed main. A continuing Household needs an Owner
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

## Future care and cross-Household exploration

**FUTURE:** CareCircle and CareCircleMembership represent people caring for a
child across Household boundaries: parents, step-parents, grandparents,
babysitters and other trusted caregivers. Potential scoped permissions include
schedule access, pickup responsibility, selected child information and temporary
caregiver access. This is separate from co-residence and is not scheduled with
the Person foundation. Medical, custody and detailed child-privacy capabilities
are not designed here.

**FUTURE EXPLORATION:** playdates and temporary coordination or trusted sharing
between families may need cross-context interaction. `HouseholdRelationship`
is not accepted, modelled or scheduled. Person relationships or temporary shared
contexts may be a better fit; the choice remains open.

## Other deferred product scope

Revisit when a concrete product flow and evidence justify it:

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
- automatic Person matching, an arbitrary social graph or a generic permission engine;
- using realtime, AI, or hardware to hide weak core workflows;
- treating deliberate hardening deferral as proof of production readiness.

## Future beta acceptance criteria — not current delivery claims

- a new Account can securely create and activate a Household;
- invited participants reach a first shared action with low friction;
- assignments/history follow explicit feature lifecycle rules when memberships
  change or are deleted, including removal of unnecessary personal references;
- tasks/routines and shopping behave predictably under retries/concurrency;
- Today is useful without mutating data;
- users can understand export/deletion/shared-data consequences;
- the product is recoverable, observable, and explicit about limitations.

The engineering order and exit gates live in the
[NEXT-STEPS](../architecture/roadmap/NEXT-STEPS.md).
