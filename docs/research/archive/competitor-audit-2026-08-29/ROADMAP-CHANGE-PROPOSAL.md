# Six-month roadmap change proposal

> **ARCHIVED RESEARCH INPUT — INTEGRATED.** Accepted deltas were incorporated
> into the current
> [six-month roadmap](../../../architecture/roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md)
> and [next steps](../../../architecture/roadmap/NEXT-STEPS.md).

Research cutoff: **2026-08-29**. This document compared with the earlier planning baseline and is retained for traceability only.

## Decision

### Current six-month roadmap: MODIFY

Keep the six-phase engineering sequence and beta scope. Add one pre-schema decision gate, move notification/calendar **semantics research** earlier, strengthen onboarding/export acceptance criteria, and keep scope-heavy implementations deferred.

The most important change is:

> Before the first durable membership schema, decide and document a stable `MembershipId`, an optional later-linkable `AccountId`, multi-household cardinality and security-only role semantics. Do not require every household person to be an authentication User.

## Current plan versus research

| Current plan | Research finding | Proposed change | Why | Priority / disposition |
|---|---|---|---|---|
| Phase 1 finishes `CreateHousehold` using UserId membership | Family Tools and Jam directly support profiles without ordinary login; Skylight is an adjacent profile analogue. FamilyWall instead proves no-email does not necessarily mean loginless; Google supports supervised child Accounts | add a P0 decision gate before mapping/migration; MembershipId + optional AccountId is recommended | avoids identity/history migration and invented UserIds | `ADD — REQUIRED NOW` |
| Phase 1 current-state text says Parent/Child enum mismatch blocks build | current code has moved: `Household(name, owner)` conflicts with handler and 10 tests | refresh roadmap baseline only when separately authorized; first restore constructor/caller/test alignment | research must not freeze stale diagnosis | `ADD NOTE — REQUIRED NOW` |
| CreatorUserId exists in command during partial slice | competitor account models and roadmap security both require individual trusted actors | keep the existing trusted-current-user correction as a hard endpoint gate | prevents impersonation/tenant breach | `KEEP — REQUIRED NOW` |
| Phase 2 Identity decides User/Profile overlap | account and membership are different lifecycles; a global Person is not yet proven | expand decision to Account ↔ Membership optional link; explicitly defer global Person | smallest model that supports children and multiple households | `MODIFY — REQUIRED NOW` |
| Phase 3 Owner/Member/Guest with generic Guest | role/relationship separation is validated; granular ACL demand is mixed | keep fixed roles and resource matrix; record relationship/capability as later questions | avoids both Cozi-style overbreadth and premature ACL platform | `KEEP + CLARIFY — REQUIRED SOON` |
| Phase 3 `GetMyHouseholds` and composite membership | multi-circle/group support is common; one Account may participate in several households | make multi-household cardinality explicit and forbid global AccountId uniqueness; switching remains client state | prevents a structural dead end without adding UI scope | `MODIFY — REQUIRED NOW` |
| Phase 3 invites/membership lifecycle | partner adoption and revocation are repeated pain points | add activation telemetry: invite issued → accepted → first shared action; prove remove/leave/transfer | product adoption and security share the same workflow | `ADD — REQUIRED SOON` |
| Phase 4 tasks/routines supports limited daily/weekday recurrence | recurrence is table stakes and a common reliability pain | keep limited rules; add edit-series, missed occurrence, DST, completion-history and idempotency acceptance cases | correct semantics matter more than broad RRULE support | `MODIFY — REQUIRED SOON` |
| Phase 4 excludes scheduler/notifications | reminders are table stakes but notification failures/noise recur | specify recipient/preferences/quiet-hours/dedup/delivery-status in Phase 4; add a simple worker only when first unattended delivery ships | moves product semantics earlier without building a platform | `ADD SEMANTICS; KEEP INFRA DEFERRED` |
| Phase 5 one active ShoppingList | shared shopping is table stakes; specialists often support several lists | keep one active list for beta, but do not hard-code a permanent one-list product contract | scope stays small while preserving evolution | `KEEP + FUTURE-PROOF` |
| Phase 5 basic Events, no external sync | duplicate entry is a major problem; sync reliability is hard | add provider-agnostic calendar integration design spike in Phase 3/4; keep OAuth/adapters/jobs later | informs Event time/provenance boundaries without delaying core | `RESEARCH FIRST — MOVE EARLIER` |
| Phase 5 Today read composition | direct/hardware products confirm daily agenda value | keep Today; make it the activation/retention surface and prove reads are side-effect free | high value, low domain coupling | `KEEP — P1` |
| Push/realtime outside six months | “live” behavior expected but transport evidence does not justify SignalR | keep infrastructure deferred; define freshness/conflict/refresh and use refetch/polling first | realtime does not solve lost updates | `KEEP DEFERRED` |
| Children/caregivers beyond Guest outside six months | loginless identity is structural, workflows are not | move only identity cardinality pre-schema; keep child/caregiver feature UI later | separates model flexibility from scope explosion | `SPLIT: MODEL NOW / PRODUCT LATER` |
| Meals/expenses/maintenance outside six months | specialists show distinct bounded languages | keep deferred; never add them as fields on Task/Household | protects context boundaries | `KEEP DEFERRED` |
| Phase 6 security/deployment hardening | household data is long-lived; Maple highlights exit risk | add machine-readable export/deletion/shared-data-fate design and at least beta-level implementation/verification | trust and lifecycle, not legal conclusion | `ADD — BEFORE PUBLIC BETA` |
| No AI in plan | Jam/Ohai/Skylight show draft-confirm ingestion; reliability is prerequisite | keep out of six months; record later provenance/review/confirm rule | avoids silent destructive automation | `KEEP DEFERRED` |
| No hardware/location/legal records | specialist/hardware products exist but carry high cost/risk | keep excluded | low thesis fit, privacy/support/operational burden | `KEEP EXCLUDED` |

## Revised sequence without silently rewriting the masterplan

### Phase 1 — same objective, one gate added

- Restore a green solution and complete the secure PostgreSQL-backed `CreateHousehold` vertical slice.
- Before EF mapping/migration, answer the ten P0 membership questions in `COMPETITOR-IMPACT-ON-DDD.md`.
- Preserve trusted actor boundary; no command/DTO `CreatorUserId` from a client.
- No children UI, Account linking UI or multi-household dashboard yet.

### Phase 2 — Identity plus account-link semantics

- Keep ASP.NET Core Identity in Infrastructure and Domain framework-independent.
- Decide whether the existing Domain `User` retires or becomes justified Profile behavior.
- Define AccountId link/unlink rules for Membership; do not introduce global Person without a use case.
- Migration tests prove optional link/filter uniqueness and pre-production data strategy.

### Phase 3 — collaboration, lifecycle and activation

- Build invitations, list households, rename, remove, leave and transfer ownership.
- Enforce resource authorization from trusted AccountId → Membership for each HouseholdId.
- Instrument household activation and invite funnel.
- Run the external-calendar problem/design spike; no provider code.
- Start with the fixed role matrix; interview caregiver/pickup cases before capabilities.

### Phase 4 — tasks/routines and semantics for reliable reminders

- Build tasks, assignment by MembershipId, completion/reopen/history and bounded recurrence.
- Add IANA timezone, DST, missed/edit-one/edit-series and occurrence idempotency tests.
- Define notification preferences/quiet hours/dedup/status.
- Only if the beta includes a reminder that must fire without a request: add the smallest scheduled job/delivery table. No broker/event bus.

### Phase 5 — shopping, Today, optional internal Events

- Keep priority: Shopping → Today → internal Events if capacity.
- Use stable item IDs and atomic/conditional item commands; refetch/polling first.
- Keep one active list initially, but avoid a permanent one-list API/schema assumption.
- Today remains a no-write Application projection.
- Internal Events remain provider-neutral.

### Phase 6 — beta trust and operations

- Keep security/load/observability/Azure work.
- Add export/deletion/shared-record-fate acceptance criteria and validate them.
- Measure list/task freshness and activation before considering realtime.
- Record post-beta decisions for calendar provider, caregiver scope and responsive kiosk—not implementations.

## Roadmap kill/exit gates

| Gate | PASS evidence | If FAIL |
|---|---|---|
| Membership semantics | ADR-ready answer for loginless member, stable ID, optional Account link, multi-household and roles | block first durable membership migration |
| First vertical slice | build/tests green; real PostgreSQL create/read; trusted actor; no source-level mismatch | do not start Identity expansion |
| Phase 3 collaboration | invite/accept/remove/leave/transfer and IDOR matrix pass | do not create Tasks/Shopping endpoints |
| Recurrence | deterministic timezone/DST/edit-series/duplicate-occurrence tests pass | ship one-time tasks only |
| Notifications | concrete reminder semantics and retry/dedup acceptance exist | do not add push/scheduler platform |
| Shopping freshness | concurrent same/different-item behavior is explicit and tested | do not add realtime transport |
| Calendar integration | repeated user problem + provider priority + mapping/conflict model | keep external sync deferred |
| Public beta trust | export/deletion fate, authz, backup/restore and incident controls verified | no public beta claim |

## Features that moved

### Move earlier

- membership identity/cardinality decision;
- activation/invite measurement;
- recurrence edge-case semantics;
- notification semantics;
- calendar-integration research/design only;
- export/deletion/shared-data-fate design.

### Move later or keep later

- global Person/Profile context;
- caregiver capabilities beyond fixed Guest;
- external calendar implementation;
- push/realtime infrastructure;
- multiple shopping-list UX if one list proves enough;
- AI, meals, expenses, maintenance, cross-household, kiosk, location.

### Remove

Nothing in the current beta core must be removed. If schedule pressure remains, follow the current plan's own rule: keep Shopping and Today, drop basic Events before weakening quality.

## Change budget

The proposal adds decision and acceptance work more than product scope. It should fit six months by explicitly **not** adding children UI, caregiver ACLs, external sync, push platform, SignalR, AI, meals, expenses or maintenance.

## Required follow-on decision

Next file to create after this research, in a separate authorized task:

`docs/architecture/adr/0006-separate-account-and-household-membership-identity.md`

The ADR should decide only the pre-schema minimum. It should not silently approve a global Person context or a general permissions engine.
