# HomePlatform feature gap analysis

> **ARCHIVED SUPPORTING EVIDENCE — NON-AUTHORITATIVE.** Research cutoff:
> 2026-08-29. Current product authority is
> [PRODUCT-SCOPE.md](../../../product/PRODUCT-SCOPE.md).

Research cutoff: **2026-08-29**. Repository snapshot: repository root at `main@a87a8176676a118a2a684f02d0a2ab7f74eef182`.

## Status definition

- `ALREADY MODELLED`: current Domain code contains the concept/behavior.
- `PLANNED`: the six-month masterplan explicitly schedules it.
- `PARTIALLY ACCOUNTED FOR`: structure or plan recognizes it but the product semantics are incomplete.
- `MISSING`: material evidence supports it and neither code nor plan adequately accounts for it.
- `INTENTIONALLY EXCLUDED`: masterplan explicitly defers it.
- `TOO EARLY TO DECIDE`: evidence or product thesis is insufficient.

No user-facing household feature is implemented end to end. “Already modelled” never means deployed, persistent or production-ready.

## What is actually covered

| Capability | Status | Current evidence | Finding |
|---|---|---|---|
| Household name/identity | `ALREADY MODELLED` | `Household.cs:5-43` | basic invariant only; persistence/API absent |
| Initial Owner | `ALREADY MODELLED` | `Household.cs:17-43` | constructor enforces Owner, but callers/tests are stale |
| Member uniqueness inside one Household instance | `ALREADY MODELLED` | `Household.cs:45-58` | in-memory UserId check; not a database race guard |
| Owner/Member/Guest vocabulary | `ALREADY MODELLED` | `HouseholdRole.cs:3-8` | no authorization behavior exists |
| Account lifecycle and trusted actor | `PLANNED` | masterplan `:360-499` | required before exposing collaborative endpoints |
| Invitations/membership lifecycle/resource authorization | `PLANNED` | masterplan `:501-633` | remove/leave/transfer/last-owner are not code |
| Tasks and limited routines | `PLANNED` | masterplan `:637-760` | evidence confirms centrality |
| Shopping, basic events and Today | `PLANNED` | masterplan `:762-877` | one active list is a start, not a permanent market assumption |
| One Account in multiple households | `PARTIALLY ACCOUNTED FOR` | duplicate check is household-local; `GetMyHouseholds` planned | avoid global uniqueness; UI switching can wait |
| Child/non-account member | `MISSING` | every member requires nonempty UserId | P0 schema question directly supported by Family Tools and Jam, with Skylight as a profile analogue; FamilyWall/Google show credentialed child alternatives |
| Stable MembershipId and later account link | `MISSING` | HouseholdMember is only UserId + Role | P0; assignments/history must not depend on credential existence |
| Relationship separate from authorization | `PARTIALLY ACCOUNTED FOR` | plan says Parent/Child are people, not roles | preserve this decision; taxonomy/capability engine can wait |
| Notification preferences/quiet hours/dedup semantics | `MISSING` | notifications explicitly deferred | product semantics required before reminders; infrastructure can wait |
| External calendar sync | `INTENTIONALLY EXCLUDED` | masterplan `:39-40`, `:796`, `:852` | keep implementation deferred; add early design spike |
| Push/realtime | `INTENTIONALLY EXCLUDED` | masterplan `:40-41` | correct; reliable refresh/concurrency first |
| Children/school/pickup/caregivers beyond Guest | `INTENTIONALLY EXCLUDED` | masterplan `:33-38` | workflows later, but membership schema must not block them |
| Meals, expenses, maintenance | `INTENTIONALLY EXCLUDED` | masterplan `:38-39` | justified P2 specialist contexts, not six-month core |
| Household-to-household sharing | `TOO EARLY TO DECIDE` | deferred connections/discovery thesis | potential differentiation; no mainstream absence claim or P0 schema change |
| AI ingestion/automation | `INTENTIONALLY EXCLUDED` by scope | no plan/code | P3 until reliable manual workflows and confirmation/provenance exist |
| Hardware/ambient display | `TOO EARLY TO DECIDE` | frontend placeholder only | responsive tablet/kiosk later; no hardware business |
| Data export/deletion/shared-record fate | `PARTIALLY ACCOUNTED FOR` | security roadmap covers account lifecycle but product semantics incomplete | required before public beta, not legal conclusion |

## Priority gaps

### P0 — structural omissions before the first durable membership schema

1. Decide whether a `HouseholdMember` may exist without an authenticated account.
2. Give membership stable identity independent of Account; make the account link optional and later attachable without history loss.
3. Permit the same Account to link to memberships in several households; never impose global `AccountId` uniqueness.
4. Make future task/list/event assignment reference `MembershipId`, not a caller-supplied or necessarily present `UserId`.
5. Keep Owner/Member/Guest strictly as authorization vocabulary; do not encode Parent/Child/Grandparent as security roles.
6. Define last-Owner mutation, membership exit and attribution invariants before Phase 3 persistence.
7. Replace caller-controlled `CreatorUserId` with the planned trusted current-user boundary before an endpoint exists.

These are small implementation changes now but a major conceptual correction. A global cross-household `Person` model is **not** P0.

### P1 — important within six months

1. A complete invite-to-first-shared-action flow, instrumented as household activation.
2. Recurring tasks with assignment, local timezone, selected weekdays, completion history and idempotent occurrence identity.
3. One fast shared shopping list with atomic item commands and recoverable refresh; keep the model extensible to multiple lists.
4. Today/agenda as an Application read composition, not an aggregate.
5. Notification semantics: recipient, preference, reminder time, quiet hours, deduplication and delivery status. Implement delivery only with the first real reminder.
6. A calendar-integration research/design spike before the Event model hardens; do not move provider implementation forward.
7. Explicit membership remove/leave/ownership transfer and immediate authorization revocation.
8. Machine-readable export/deletion design and shared-data fate before public beta.

### P2 — later opportunities

- caregiver/grandparent scopes after fixed roles are proven inadequate;
- pickup/dropoff requests and driver/bring-item logistics;
- external calendar one-way import, then carefully scoped two-way sync;
- maintenance as its own asset/service-history language;
- meals/grocery generation;
- expenses with explicit shared-record ownership;
- multi-household Today or intentionally shared events;
- tablet/kiosk presentation;
- confirmed-draft ingestion from email/photo/PDF/voice.

### P3 — do not build now

- location tracking or safety promises;
- public household discovery/profiles;
- court-grade immutable communication records;
- payments/allowance/cards/reward marketplace;
- hardware manufacturing/subscription dependency;
- generic permissions engine before concrete capability cases;
- unrestricted RRULE designer;
- AI that mutates household state without human confirmation;
- SignalR/WebSockets, Redis, broker, microservices or event sourcing without a measured trigger;
- full meal, finance or home-maintenance suites inside Household/Task aggregates.

## Feature decision matrix

| Feature | Competitor evidence | User problem | Class | HomePlatform status | Build window | Domain impact | Architecture impact | Security/privacy impact | Recommendation |
|---|---|---|---|---|---|---|---|---|---|
| Individual accounts | almost universal; Cozi is negative outlier | accountability and safe revocation | Table stakes | Planned | Phase 2 | Account identity | Identity boundary | credential/session lifecycle | build as planned |
| Household creation | all direct products | create shared context | Table stakes | partial/broken slice | Phase 1 | Household root | vertical slice/persistence | trusted creator | finish after P0 decision |
| Invite/join | direct, platform and co-parent products | second person reaches value | Table stakes | Planned | Phase 3 | Invitation lifecycle | idempotent acceptance | token expiry/reuse/IDOR | build and instrument |
| Non-account child/dependent | Family Tools, Jam; Skylight profile analogue | represent family before login | Table stakes for family scope | Missing | decision now; UI later | stable MembershipId + optional AccountId | filtered uniqueness/link use case | guardian/access/data rights | fix schema concept now |
| Several households/circles | TimeTree, FamilyWall, AppClose, Splitwise | blended/extended/care networks | Competitive advantage | partial | schema now; UX later | membership per Household | authorization by HouseholdId | cross-tenant isolation | permit; do not add active-household domain state |
| Role vs relationship | specialist/direct role models | authority differs from family relation | Structural | partial | Phase 3 | authorization role; optional later relationship | policy tests | least privilege | preserve separation; no ACL engine yet |
| Remove/leave/transfer | platform/co-parent flows | safe membership lifecycle | Table stakes | Planned | Phase 3 | last Owner; history attribution | concurrency transaction | immediate revocation | build explicitly |
| Shared calendar | Cozi, FamilyWall, Jam, TimeTree, hardware/platforms | coordinate time | Table stakes | basic Events planned | Phase 5 | Event root | read/query model | audience/time data | keep basic internal event optional after core |
| External calendar sync | FamilyWall, Any.do, Jam, Skylight, Google | avoid duplicate entry | Competitive advantage / expectation | excluded | spike Phase 3/4; build later | provider provenance outside Event | adapter, cursor, idempotent upsert | OAuth scope/content leakage | research first; do not move implementation earlier |
| Tasks/chores | direct and chore specialists | allocate work | Table stakes | Planned | Phase 4 | HouseholdTask root | focused repository/query | resource authorization | build |
| Recurrence | task/calendar/chore specialists | reduce re-entry | Table stakes | Planned | Phase 4 | RecurrencePattern; occurrence identity | timezone/DST/idempotency | notification consequences | support limited rules well |
| Assignment | direct/task/chore products | clear responsibility | Table stakes | Planned conceptually | Phase 4 | references MembershipId | authorization/query indexes | child visibility | build |
| Completion history | specialists/co-parent/expense products | trust and accountability | Competitive advantage | partial plan | Phase 4 | immutable attribution facts, not event sourcing | append/history queries | retention/child data | keep minimal history |
| Today/agenda | direct, AI and hardware products | reduce cognitive load | Table stakes | Planned | Phase 5 | no aggregate | Application read composition | only authorized projection | build; make activation surface |
| Shopping list | direct and shopping specialists | coordinate frequent errands | Table stakes | Planned | Phase 5 | ShoppingList/item | atomic item commands | household access | build; one list initially, no permanent cap |
| Live visibility | most list/calendar products claim sync | see another member's change | Table stakes behavior, not transport | missing | Phase 5 semantics | none | refetch/polling/conditional updates first | no data leakage in fanout | no SignalR until measured |
| Offline/retry | task/list products; repeated failure reports | weak connectivity | Competitive advantage | missing | post-beta unless client demands | command identity | pending queue/replay/idempotency | device data | specify truthful state; do not claim offline-first |
| Notifications/reminders | most direct/task products | action at the right time | Table stakes | excluded infra; semantics missing | specify Phase 4; deliver when first reminder ships | preference/reminder concepts | scheduler/job then outbox only if required | quiet hours, child/device consent | move semantics earlier, not platform tech |
| Multiple shopping lists | AnyList/Bring and specialists | stores/topics differ | Competitive advantage | one active list planned | later Phase 5/P2 | list lifecycle | unique/index changes | low | keep future-compatible, do not force now |
| Meals/recipes | Cozi, FamilyWall, AnyList, Paprika, Mealime | weekly planning | Optional/P2 | excluded | after core | separate Meals language | integration/read composition | dietary/health inference | defer |
| Expenses | FamilyWall, Splitwise, co-parent apps | shared money/accountability | Specialist/P2 | excluded | after core | Expense/Settlement context | financial integrations later | sensitive financial data | defer |
| Maintenance | Dwellin, HomeZada, Centriq | asset/service lifecycle | Potential differentiator/P2 | excluded | after core | Asset/ServiceRecord context | document storage/reminders | documents/home data | research later; never task fields only |
| Pickup/dropoff requests | Jam, OFW, AppClose | coordinate child logistics | Potential differentiator/P2 | excluded | after core | Request/accept/decline lifecycle | notification/invite boundary | child/location sensitivity | validate segment before build |
| Household-to-household share | multi-circle/co-parent partial analogues | coordinate trusted networks | Potential differentiator | too early | discovery after core | access grant/link concept | cross-household ACL/revocation | high boundary risk | research, not schema now |
| Caregiver scope | Jam, AppClose, OFW, FamilyWall | limited access for non-core adults | Specialist/P2 | generic Guest planned | validate after Phase 3 | capability or resource grant if proven | policy matrix | least privilege/child data | start with Guest; add only from workflows |
| AI draft ingestion | Jam, Ohai, Skylight | reduce manual re-entry | Optional/P3 | missing/excluded | later | provenance/confirmation | parser/provider boundary | content/processors | only draft-review-confirm after reliable core |
| Rewards/gamification | Sweepy, Nipto, S'moresUp, hardware | child motivation | Specialist/P3 | missing | later | points/reward economy | more state/abuse handling | child manipulation/data | do not build now |
| Location | FamilyWall, Life360, co-parent check-ins | safety/coordination | Specialist/P3 | excluded | not in six months | separate consent/location model | high-risk provider/storage | precise location/child safety | do not build now |
| Export/portability | Maple, Splitwise, co-parent products | retain long-lived history and exit | Table stakes trust | partial security planning | design before beta | ownership/attribution | export job/read model | subject vs household rights | add roadmap acceptance criteria |
| Wall display | Skylight, Hearth, Family Kiosk, DAKboard, Mango | ambient shared awareness | Optional/P2 | no frontend | after responsive product | none | client presentation/cache | local device exposure | software kiosk only if adoption proves it |

## Coverage score

- **Implemented product coverage: 2/10.** Domain skeletons and health/readiness exist, but no persistent or HTTP household workflow works.
- **Coverage if the unchanged six-month plan were delivered: approximately 6/10 for the chosen beta thesis.** It would cover the core loop but retain a membership identity risk and weak notification/calendar-integration preparation.
- **Coverage if this proposal is adopted: target 7/10 without adding scope-heavy modules.** The improvement comes from correcting identity, lifecycle, reliability semantics and onboarding—not from copying meals, expenses, location or AI.
