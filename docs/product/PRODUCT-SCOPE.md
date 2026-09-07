# HomePlatform Product Scope

Status: **Authoritative product scope for the six-month beta**  
Last reviewed: **2026-09-07**

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
| Account lifecycle | register, verify, sign in, recover, revoke | trusted framework identity; no custom auth protocol |
| Household | create, rename, list | protected collaboration boundary |
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
- One Account may need Memberships in several Households.
- Owner/Member/Guest express authorization. Parent/Child/Partner/Grandparent
  describe relationships and do not automatically grant access.
- Today is a read surface over several modules, not a new write model.
- Recurrence, reminders, and collaboration quality matter more than a long
  checklist of shallow features.

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
- assignments and history survive approved Membership lifecycle changes;
- tasks/routines and shopping behave predictably under retries/concurrency;
- Today is useful without mutating data;
- users can understand export/deletion/shared-data consequences;
- the product is recoverable, observable, and explicit about limitations.

The engineering order and exit gates live in the
[masterplan](../architecture/roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md).
