# HomePlatform Competitive Product & Architecture Audit

> **ARCHIVED SUPPORTING EVIDENCE — NON-AUTHORITATIVE.** Research cutoff:
> 2026-08-29. Use the concise
> [competitor summary](../../../product/COMPETITOR-SUMMARY.md) and current
> architecture/roadmap documents for decisions.

Audit ID: `HOMEPLATFORM-COMPETITOR-PRODUCT-ARCHITECTURE-AUDIT-01`  
Research cutoff: **2026-08-29**  
Repository inspected: repository root  
Repository snapshot: `main@a87a8176676a118a2a684f02d0a2ab7f74eef182`  
Mode: research and read-only repository audit; source code, masterplan and ADRs were not modified.

## Executive Summary

**TOTAL PRODUCTS DISCOVERED:** 46

**DEEP-DIVE PRODUCTS:** 15 — Cozi, FamilyWall, Family Tools, Jam, TimeTree, Any.do Family, Ohai, Sweepy, Skylight, OurFamilyWizard, AppClose, AnyList, Splitwise, Dwellin and Google Family.

**CLOSEST DIRECT COMPETITORS:** FamilyWall, Family Tools, Cozi and Jam. TimeTree and Any.do are the strongest adjacent coordination references.

**CURRENT FEATURE COVERAGE:** 2/10 implemented. Household/member skeletons and health/readiness exist, but no persistent or HTTP household workflow works. The current plan would reach roughly 6/10 for a deliberately narrow beta if delivered unchanged.

**BIGGEST PRODUCT OMISSIONS:** loginless/dependent membership with later account linking; explicit multi-household cardinality; notification semantics; trusted sync/freshness behavior; calendar-integration design; activation/onboarding measurement; shared-data export/deletion fate.

**BIGGEST ARCHITECTURAL RISKS:** equating membership with required UserId; trusting caller-supplied actor ID; no database/concurrency proof; recurrence/time/idempotency complexity; future provider-sync and reliable-delivery semantics.

**MOST IMPORTANT DDD DISCOVERY:** `Account` and `HouseholdMember` have different identities and lifecycles. The smallest defensible fix is a stable `MembershipId` with optional later-linkable `AccountId`. A global `Person` context is not yet justified.

**STRONGEST DIFFERENTIATION OPPORTUNITY:** calm, trustworthy daily coordination for real household structures—including members without login and adults participating in several households—without copying surveillance, hardware, finance, legal-record or AI scope.

**PORTFOLIO ARCHITECTURE SCORE:** 4.5/10 today; credible 8/10 potential after a green full slice, the membership correction and real PostgreSQL/security/concurrency proof.

**VERDICT:** Keep the modular monolith and beta scope. Modify the roadmap before locking it: resolve membership identity before the first schema, then add semantics/acceptance gates rather than more feature modules.

## Market Landscape

The sourced inventory contains:

- 10 direct family/household organizers;
- 4 adjacent coordination products;
- 24 specialist products across chores, groceries/meals, expenses, maintenance, care and co-parenting;
- 8 platform/wall-display/super-app approaches.

The complete 46-product inventory and official links are in [SOURCES.md](SOURCES.md). Three patterns matter more than feature counts:

1. **General organizers converge on calendar + tasks + lists + reminders + daily overview.** Scope beyond that varies sharply.
2. **Specialists expose deeper workflow-specific language and lifecycles.** AnyList (shopping/meal), Sweepy/Tody (chores), Splitwise (expenses) and Dwellin (maintenance) each make the repeated workflow more explicit; no outcome benchmark was available.
3. **Identity models vary and create product constraints.** Cozi's shared password/full access, Google's one-family group, Family Tools' linked profiles and AppClose's circles/non-user links are architectural product choices, not onboarding details.

Maple's announced 2026 shutdown is a useful exit/portability fact. It does not make Maple a stable benchmark and should not be converted into a prevalence claim.

## Competitor Shortlist

| Product | Role in audit | Strongest evidence | Main warning |
|---|---|---|---|
| Cozi | closest direct scope | household entitlement and core loop | shared password, unrestricted access, one-account email constraint |
| FamilyWall | broad direct suite | credentialed child without email, circles, calendar/lists/meals/budget | breadth, location risk, save/sync complaints |
| Family Tools | identity/permission reference | linked profiles, role presets, later account upgrade | calendar/notification reliability reports |
| Jam | caregiver and AI-intake reference | role/calendar scope; draft-review-confirm assistant | high price; core architecture unknown |
| TimeTree | multi-context/calendar reference | many shared calendars, current bug log, public tech evidence | flat permission semantics; per-account premium |
| Any.do Family | private/shared workspace reference | personal vs family space, roles, tasks/calendar/grocery | four-member/project caps and sharing expectations |
| Ohai | AI household assistant reference | multi-input planning/ingestion | role/security/action semantics insufficiently public |
| Sweepy | recurring chores reference | workload, schedule, child approval/history | sync/offline reliability reports; gamification is specialist |
| Skylight | ambient coordination reference | wall Today, profiles, calendar/tasks/lists/meals, intake | hardware/subscription and calendar-person mapping pain |
| OurFamilyWizard | restricted roles/lifecycle | co-parent schedule/change/record and child/third-party access | legal immutability/deletion and per-parent pricing do not generalize |
| AppClose | circles/non-user reference | third-party/non-user events, requests, records and revocation | legal/payment scope is specialist |
| AnyList | shopping/meal specialist | extremely short real-time shared-list loop | child/permission semantics unknown |
| Splitwise | multi-group/history reference | group recurrence, shared attribution and export | free daily limit; expense mutability is domain-specific |
| Dwellin | maintenance context reference | asset, manual, warranty and service lifecycle | not part of daily beta loop |
| Google Family | platform baseline | child accounts and automatic family calendar | one family group and platform-manager assumptions |

Profiles with onboarding, pricing, pain, privacy and architecture evidence are in [COMPETITOR-PROFILES.md](COMPETITOR-PROFILES.md).

## Feature Matrix

The machine-readable comparison is [COMPETITOR-FEATURE-MATRIX.csv](COMPETITOR-FEATURE-MATRIX.csv).

Legend:

- `Y`: source-backed support;
- `P`: partial, limited, paywalled or narrower than the column;
- `X`: explicit limitation/negative support;
- `U`: unknown from the evidence; not an absence claim.

The matrix deliberately avoids using `N` for undocumented features. Each row contains source IDs from [SOURCES.md](SOURCES.md).

## Workflow Comparison

| Workflow | Best evidence/pattern | Important edge case | HomePlatform consequence | Evidence |
|---|---|---|---|---|
| Create household | direct products create one shared context; Any.do separates private/shared | creator becomes authority; current constructor/caller mismatch | finish one trusted-actor vertical slice after P0 model gate | S002, S007, S011, S027 |
| Invite partner | email/link invites are common | expiry, reused link, wrong account, second person never completes | explicit Invitation lifecycle + activation measurement | S007, S016, S027 |
| Add child | Family Tools/Jam support a no-login profile; Skylight is an adjacent profile analogue; FamilyWall/OFW support no-email account paths; Google uses a supervised child Account | profile later receives login, or a credentialed/supervised Account changes lifecycle | stable MembershipId + optional verified Account link; do not conflate no-email or supervised Account with loginless member | S011, S016, S034, S040, S054, S088 |
| Add grandparent/caregiver | Jam/AppClose/OFW have scoped participant patterns | relationship is not authority; selected resources only | begin with Guest; validate capability case later | S016, S040, S043 |
| Create task | direct/task products use short title/assignee/due flow | caller must be authorized in Household | HouseholdTask root; MembershipId assignment | S005, S011, S015, S027, S032 |
| Create recurring chore | Family Tools/Sweepy/Tody/Any.do expose repeat/schedule | DST, missed occurrence, edit one/series | limited RecurrencePattern and idempotent occurrence key | S013, S027, S032, S058 |
| Assign chore | common in task/chore products | child/no-account assignee; multiple assignees | assignment references MembershipId; decide multiplicity explicitly | S011, S032, S057 |
| Complete chore | specialists keep history/approval/points | double tap, offline retry, reopen, approval | atomic/idempotent transition; minimal attribution history | S032, S057, S082 |
| Create event | shared calendars use internal event then reminders | audience, local time, recurrence and provider provenance | provider-neutral Event; time semantics before schema | S005, S015, S019, S055 |
| Coordinate pickup | Jam/OFW/AppClose use request/driver/change flows | accept/decline, custody/visibility, location | P2 explicit request lifecycle, not a Task flag | S015, S040, S042, S043 |
| Add shopping item | AnyList/Bring use immediate add/check sync | simultaneous same/different item, offline retry | stable item IDs, atomic commands, refetch first | S045, S047, S060 |
| Plan week | direct/AI/wall products combine calendar/tasks/meals | too much setup and stale inputs | Today/agenda first; weekly planner later | S005, S015, S030, S034 |
| View Today | direct and hardware products reduce cognitive load | household/local date and authorization | Application read composition; never aggregate | S015, S030, S034 |
| Remove member | circle/platform products distinguish linked access | historical attribution and last Owner | revoke access, retain Membership attribution, concurrency guard | S002, S040, S043, S054, S092 |
| Leave household | TimeTree establishes a self-leave/succession lifecycle for a shared context; other products were less explicit | account survives; household must keep Owner | model leave as a separate HomePlatform use case; one Account can retain other memberships | S092, S093 |
| Transfer ownership | manager/creator products reserve authority; TimeTree has succession rather than a general transfer workflow | simultaneous leave/demote/transfer | Household invariant + optimistic/database race test | S043, S054, S055, S092 |

Happy paths alone are insufficient. The aggregate pressure tests cover the important edge cases in [AGGREGATE-PRESSURE-TEST.md](../../../architecture/research/AGGREGATE-PRESSURE-TEST.md).

## User Pain

The full machine-readable analysis is [USER-PAIN-MATRIX.csv](USER-PAIN-MATRIX.csv). Frequency indicates recurrence of a failure mode in the collected evidence, not user-population prevalence.

Strongest recurring signals:

1. **Sync and stale/disappearing state — HIGH.** TimeTree's official incident register plus current Sweepy, Skylight, Bring and other reports show that trust collapses when shared changes are late, duplicated or missing.
2. **Paywall/repricing frustration — HIGH in complaint samples.** Core reminders, history, sync or daily actions are often used as conversion levers. Multi-account/per-parent pricing increases family adoption friction.
3. **Sharing/role mental-model mismatch — MEDIUM-HIGH.** One shared credential is too broad; board-by-board sharing and rigid group caps can be too fragmented.
4. **Notifications missed/duplicated/noisy — MEDIUM.** A push call is not a notification model.
5. **Manual re-entry and weak import — MEDIUM.** Calendar/email/photo intake matters because household information originates elsewhere.
6. **Offline/reconnect ambiguity — MEDIUM.** A mutation must not look committed when it is still pending or failed.

HomePlatform should treat reliability as product behavior: atomic commands, idempotency, visible pending/failure state, refresh/conflict semantics and honest offline claims.

## Table Stakes

### Table stakes

- individual authenticated accounts and safe revocation;
- create/invite/join/remove/leave/ownership lifecycle;
- shared calendar or clear schedule surface;
- tasks/chores, assignment, bounded recurrence and reminders;
- shared shopping list with trustworthy state;
- Today/agenda;
- per-person/context colors or filtering where several calendars/members exist;
- multi-device freshness, even if implemented by refetch/polling;
- transparent household-level value/pricing;
- data export/deletion semantics before public beta.

### Competitive advantage

- loginless member with later account link;
- one account in several households/circles;
- private/personal versus household-shared context;
- low-friction invite-to-first-value onboarding;
- caregiver/grandparent scope after validation;
- pickup/dropoff/driver/bring-item requests;
- strong recovery from sync/offline conflicts.

### Specialist

- chore approval/points/allowance;
- co-parent immutable records and legal exports;
- expense splitting/settlement;
- maintenance assets/warranties/service history;
- meal/recipe planning;
- location/safety tracking.

### Optional

- tablet/kiosk presentation;
- several shopping lists;
- weekly planner;
- document/photo/email draft ingestion;
- household digest.

### Distraction

- hardware manufacturing;
- location tracking in the core;
- full social feed/gallery;
- court-grade record system;
- autonomous AI planner;
- finance/payment rails;
- generic marketplace/rewards economy.

### Potential HomePlatform differentiator

Flexible, trustworthy coordination across real household structures is evidence-backed. Generic public/private household profiles and household-to-household discovery are only a whitespace hypothesis; they require interviews and a pilot, not architecture today.

## HomePlatform Feature Gaps

### P0 structural

- stable MembershipId independent of authentication;
- optional, later-linkable AccountId;
- multi-household cardinality without global uniqueness;
- role strictly separate from family relationship;
- last-Owner/leave/remove/link invariants;
- trusted current actor, never frontend CreatorUserId.

### P1 six-month capabilities

- complete invite and activation loop;
- tasks/routines with recurrence/time/history;
- shopping with correct concurrent item semantics;
- Today;
- notification semantics and smallest required delivery implementation;
- provider-neutral Event plus early calendar-integration design spike;
- export/deletion/shared-data fate by beta.

### P2 later

- caregiver/pickup flows;
- one-way calendar import then scoped two-way sync;
- maintenance, meals, expenses;
- multi-household shares;
- software kiosk;
- draft-confirm ingestion.

### P3 do not build now

- location, public discovery, autonomous AI, hardware, legal records, payments/allowance, generic ACL, broad RRULE engine, SignalR/Redis/broker/microservices.

The full status and feature decision matrix is [FEATURE-GAP-ANALYSIS.md](FEATURE-GAP-ANALYSIS.md).

## Household / Identity Model Findings

Current code makes `HouseholdMember == required UserId + Role`. That cannot semantically represent a child before login or preserve identity/history when an account is later attached.

Recommended conceptual minimum:

```text
Account (Identity & Access)
        |
        | optional, verified link
        v
HouseholdMember
  MembershipId
  HouseholdId
  AccountId?
  Role: Owner | Member | Guest
```

Important restraint:

- do not add global Person/profile matching now;
- do not create server-side “active household” domain state merely for UI switching;
- do not build an arbitrary capability engine;
- future assignments/history refer to MembershipId;
- family relationship, if later stored, never grants authority automatically.

Explicit domain questions and vocabulary are in [COMPETITOR-IMPACT-ON-DDD.md](../../../architecture/research/COMPETITOR-IMPACT-ON-DDD.md).

## Bounded Context Findings

Initial language/ownership boundaries:

- Identity & Access;
- Households;
- Tasks & Routines;
- Shopping;
- Events;
- Today as Application read composition.

Notifications and Calendar Integration begin as explicit semantics/ports/supporting modules and become contexts only when their independent lifecycle justifies it. People/Profile likewise waits for a proven cross-household profile.

Meals, Expenses, Maintenance, Care Logistics and Household Connections are credible later contexts because their language/invariants differ. They are not extra fields on HouseholdTask.

See the archived [PROPOSED-BOUNDED-CONTEXTS.md](PROPOSED-BOUNDED-CONTEXTS.md) and current [CONTEXT-MAP.md](../../../architecture/target/CONTEXT-MAP.md).

## Aggregate Findings

- **Household:** remains defensible as root for small membership and last-Owner invariant; membership mutations may load members, ordinary product actions must not.
- **Invitation:** separate expiry/revocation/acceptance lifecycle.
- **HouseholdTask:** separate root; assignment by MembershipId.
- **Routine:** separate root with bounded RecurrencePattern/occurrence identity.
- **ShoppingList:** root with stable item entities; avoid coarse list-wide conflicts for unrelated items.
- **HouseholdEvent:** provider-neutral root.
- **Today:** not an aggregate.

No giant Household aggregate and no generic repository/UoW/base-aggregate framework are justified.

## Architecture Pressure Test

The 12 required scenarios were evaluated in [AGGREGATE-PRESSURE-TEST.md](../../../architecture/research/AGGREGATE-PRESSURE-TEST.md).

Summary:

| Scenario | Current model | Required evolution |
|---|---|---|
| adults + children + grandparents | fails semantically | loginless Membership and later Account link |
| Account in three households | structural hint only | per-household membership uniqueness/authz |
| 10k households edit lists | unsupported | item-level atomic/conditional updates and load proof |
| recurring work | planned only | timezone/DST/idempotent occurrence |
| external event import | deferred | anti-corruption mapping/cursor/retry later |
| reliable push | absent | preferences, schedule, dedup; worker/outbox only when triggered |
| restricted grandparent | planned Guest may be coarse | validate narrow capability after fixed roles |
| leave | absent | revocation + retained attribution |
| last Owner leaves | absent | aggregate invariant + race proof |
| two edit same task | absent | explicit conflict/conditional transition |
| Household A shares to B | absent | later access grant/revocation concept |
| child gains login | fails | atomic optional Account link preserving MembershipId |

The modular monolith remains a plausible target architecture across the twelve paper scenarios; runtime and load behavior remain unverified. The current membership identity does not survive the child-linking scenarios.

## Competitor Architecture Evidence

Only evidence-backed claims are included in [ARCHITECTURE-EVIDENCE.md](ARCHITECTURE-EVIDENCE.md).

- TimeTree: `CONFIRMED` historical stack and 2026 data/search migration direction.
- Todoist: `CONFIRMED` public sync/idempotency semantics; private core stack `UNKNOWN`.
- Splitwise: Android `CONFIRMED`; Rails backend `STRONGLY INDICATED` only.
- Bring!: AWS/integrations `CONFIRMED`; core `UNKNOWN`.
- Most core competitor architectures: `UNKNOWN`.

Nothing in the evidence justifies changing .NET 10, PostgreSQL or the modular monolith.

## Security & Privacy

Strong product-level lessons:

- never use a shared household password;
- authenticate individuals and authorize every resource by current Household membership;
- distinguish remove member, leave household, delete account and delete household;
- loginless child profiles require guardian/authority semantics before public exposure;
- export/deletion must define subject data versus shared household records;
- location, school/pickup, calendar contents, expenses and documents are elevated-risk data;
- “encrypted” vendor claims must not be restated as E2EE or independent assurance;
- no public security page is proof of live controls.

The existing security roadmap's trusted actor, Infrastructure-owned Identity, resource-scoped roles and PostgreSQL authorization direction is sound. Product research adds membership-link and shared-data-lifecycle questions; it does not offer legal conclusions.

## Realtime Requirements

User-visible freshness is table stakes. SignalR/WebSockets are not.

Recommended progression:

1. stable resource/item IDs;
2. atomic commands and database constraints;
3. optimistic UI with truthful pending/failure state;
4. refetch after mutation and on focus; short polling for active list if needed;
5. conditional/version conflict only where a real lost-update rule exists;
6. measure staleness and collaboration latency;
7. add SignalR only if a validated sub-second requirement remains and polling/refetch fails.

Push notifications are for attention, not shared-state synchronization. Realtime transport does not solve concurrency.

## Recurrence / Scheduling Requirements

Observed behavior is narrower than the architecture questions:

| Product | Publicly evidenced behavior | Future instances | Timezone/DST | Missed occurrence | Edit one vs series | Completion history | Evidence |
|---|---|---|---|---|---|---|---|
| Family Tools | weekly/monthly chore repeat and up to several reminders | U | U | U | U | P | S013 |
| Sweepy | task frequency, generated daily schedule and history/approval | P | U | U | U | Y | S032, S033 |
| Tody | needs/frequency schedule, rotations and household history | P | U | U | U | Y | S058, S081 |
| TimeTree | recurring calendar events and reminders | P | U | U | U | U | S019, S025 |
| Any.do | recurring tasks/reminders, including advanced patterns by plan | P | U | U | U | P | S026, S027 |
| Splitwise | weekly/fortnightly/monthly/yearly recurring expenses with recurrence history | P | U | P | U | Y | S049 |
| Jam | recurring to-dos and calendar events | P | U | U | U | P | S015–S017 |
| Skylight | recurring chores/routines and calendar events | P | U | U | U | P | S034 |

`U` is important: public product evidence did not establish consistent DST, missed-occurrence or edit-one/edit-series rules. The following HomePlatform behavior is therefore a recommendation derived from failure risk, not a claim that competitors implement it well.

Phase 4 should support a deliberately small recurrence language:

- daily and selected weekdays;
- local time plus IANA timezone;
- defined DST gap/overlap behavior;
- activation/pause/end semantics;
- missed occurrence behavior;
- edit one occurrence versus this-and-future/series;
- completion history and reopen rules;
- unique `(RoutineId, OccurrenceKey)` or equivalent idempotency;
- reads never generate rows.

Do not implement arbitrary RRULE, cron design, years of future instances or a general scheduler. A worker is required only when generation/reminder must happen without user traffic.

## Notification Requirements

Competitor comparison:

| Product | Reminders/notifications | Preferences | Quiet hours | Digest | Child rules | Escalation | Reliability evidence |
|---|---|---|---|---|---|---|---|
| Family Tools | Y; multiple chore reminders | P | U | U | P through roles | U | missing notifications reported; S013, S014 |
| TimeTree | Y | P | U | U | U | U | duplicate/delayed notifications in official bug log; S025 |
| Any.do | Y | P | U | U | U | U | in-app/email behavior plus cross-device reminder-loss report; S029, S094 |
| Sweepy | U | U | U | U | U | U | schedule/reconnect reports do not establish a notification channel; S033 |
| FamilyWall | Y | P | U | U | P | U | overload/missing notifications reported; S009 |
| OurFamilyWizard | Y | P | U | U | Y through restricted account visibility | P through co-parent requests | S039–S041 |
| AppClose | Y | P | U | U | Y through circle/third-party scope | P through requests | S042–S044 |
| Skylight | Y | P | U | U | P through profiles/routines | U | direct task-due reminders plus sync/profile mapping reports; S034, S038, S096 |

Quiet hours, digest and escalation were mostly `UNKNOWN`; they remain product questions. The architecture requirements below are recommendations, not inferred competitor implementation.

Before choosing a push provider, define:

- logical notification type and source;
- recipient Membership/linked Account/device/channel;
- reminder time and timezone;
- per-person preferences;
- quiet hours and digest choice;
- child/guardian rules;
- deduplication/idempotency key;
- scheduled, claimed, sent, failed, cancelled status;
- retry/backoff and revocation behavior.

Start with explicit Application orchestration. Add a scheduled-delivery table/worker with the first unattended reminder. Add a same-database outbox only when commit-to-delivery handoff must be crash-safe. No broker is currently justified.

## Monetization

Observed models include free/ad-supported, freemium, household subscription, per-account, per-parent and hardware + subscription.

Common paywalls:

- external calendar sync and history/search;
- multiple reminders/advanced recurrence;
- household sync/more members or spaces;
- meal/budget/location modules;
- AI import/automation;
- hardware screensavers/rewards/meal features;
- unlimited expense entry/export.

Strategic inference:

- a household product should prefer one understandable household entitlement over per-user/per-parent multiplication;
- the free/trial loop must reach a second member and one shared success before payment;
- do not paywall correctness, account security, data export or basic recovery;
- pricing pages conflict by region/channel, so this audit does not propose a final price.

## Onboarding

The requested eight-step comparison uses `Y/P/U/X` with the same evidence discipline as the feature matrix:

| Product | Account | Household/space | Invite partner | Add child | First task | First event | First shopping item | Today/dashboard |
|---|---|---|---|---|---|---|---|---|
| Cozi | Y | Y | P | P | Y | Y | Y | Y |
| FamilyWall | Y | Y | Y | Y | Y | Y | Y | Y |
| Family Tools | Y | Y | Y | Y | Y | Y | Y | Y |
| Jam | Y | Y | Y | Y | Y | Y | Y | Y |
| TimeTree | Y | P | Y | U | U | Y | U | Y |
| Any.do Family | Y | Y | Y | U | Y | Y | Y | Y |
| Ohai | Y | P | P | U | Y | Y | P | Y |
| Sweepy | Y | Y | Y | P | Y | U | U | Y |
| Skylight | Y | Y | Y | Y | Y | Y | Y | Y |
| OurFamilyWizard | Y | Y | Y | Y | P | Y | U | Y |
| AppClose | Y | Y | Y | Y | P | Y | U | Y |
| AnyList | Y | P | Y | U | U | U | Y | P |
| Splitwise | Y | Y | Y | U | U | U | U | P |
| Dwellin | Y | Y | Y | U | Y | P | U | Y |
| Google Family | Y | Y | Y | Y supervised Account | U | Y | P | P |

FamilyWall, Family Tools, Jam and Skylight cover all eight steps most directly, but that does not prove low friction. Public pages expose steps, not conversion time. Jam's and Skylight's intake can reduce re-entry; Family Tools has the clearest later-account-upgrade path. No product supplied comparable funnel data, so “best onboarding” remains an inference.

The fastest credible HomePlatform first-use path is:

1. create individual account;
2. create Household and automatically link creator as Owner Membership;
3. create one optional loginless member or invite partner;
4. partner accepts and receives their own Account/Membership link;
5. choose one optional starter template;
6. create/assign first responsibility or add first shopping item;
7. both see the change in Today/list;
8. prompt calendar connection research only after core value, not during setup.

Measure:

- household created → invitation issued;
- invitation issued → accepted;
- accepted → first shared mutation;
- time to second-member value;
- week-one repeated task/list use;
- invite/link/remove failures.

Do not require children, meals, calendar OAuth, detailed family relationships or permission customization before first value.

## Product Opportunities

The scored map is [PRODUCT-OPPORTUNITY-MAP.md](PRODUCT-OPPORTUNITY-MAP.md).

Highest-value feasible opportunities:

1. trustworthy tasks/routines + Today;
2. fast shared shopping;
3. loginless membership with later account link;
4. multi-household-compatible membership;
5. invite-to-first-value activation;
6. clear household entitlement;
7. notification semantics without premature platform infrastructure.

Cross-household collaboration remains a discovery hypothesis, not a six-month commitment.

## Six-Month Roadmap Impact

Decision: **MODIFY**, do not replace.

Keep:

- Phase 1 vertical slice;
- Phase 2 Identity/trusted actor;
- Phase 3 membership/invitation/authz;
- Phase 4 tasks/routines;
- Phase 5 shopping/Today, Events only if capacity;
- Phase 6 security/operations/Azure;
- no microservices, broker, Redis, event sourcing, MediatR or generic repository.

Modify:

- add the P0 MembershipId/optional Account/multi-household/role gate before migration;
- update the stale current-state diagnosis in a separately authorized masterplan revision;
- measure invite activation in Phase 3;
- add recurrence edge semantics and notification semantics in Phase 4;
- run a calendar-integration design spike in Phase 3/4, not implementation;
- keep the shopping model future-compatible with several lists;
- add export/deletion/shared-data-fate acceptance before beta.

See the archived [ROADMAP-CHANGE-PROPOSAL.md](ROADMAP-CHANGE-PROPOSAL.md) and current [six-month roadmap](../../../architecture/roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md).

## Portfolio DDD Review

**Current score: 4.5/10.**

Strengths:

- clear dependency direction;
- actually framework-independent Domain;
- pragmatic avoidance of ceremonial frameworks;
- real PostgreSQL/Testcontainers foundation for health/readiness;
- existing masterplan/ADR discipline;
- planned resource authorization and concurrency gates.

Weaknesses:

- source-level constructor mismatch prevents a green solution;
- no complete persistent/API/authenticated use case;
- Account/person/member language is not defensible for likely workflows;
- Domain behavior is too small to demonstrate serious DDD;
- tests do not prove EF mapping, resource authorization, aggregates, races or recurrence;
- untracked masterplan baseline is already stale.

Five largest improvements:

1. Restore green build/tests and finish one trusted PostgreSQL-backed vertical slice.
2. Correct membership identity before schema.
3. Prove last-Owner, invite, leave/remove and authorization invariants.
4. Add real task/routine lifecycle with justified value objects and PostgreSQL concurrency/idempotency tests.
5. Prove migrations, security, export/deletion and delivery behavior; add events/outbox only from real triggers.

The project can become an excellent DDD portfolio precisely by explaining why it did **not** add layers/patterns without business rules.

## Things NOT To Build

1. Location tracking, geofences or emergency-safety claims.
2. Autonomous AI or unreviewed email/photo/PDF mutation.
3. Hardware, rewards/allowance economy or family social feed.
4. Court-grade immutable records, calls, payments or legal exports.
5. Meals, expenses, maintenance and household discovery inside the first beta.
6. Global Person matching or generic ACL engine before validated workflows.
7. SignalR/WebSockets, Redis, distributed cache, queue/broker, microservices, event sourcing, MediatR, generic repository or UoW wrapper without the documented trigger.

## Evidence Limitations

- Review/community samples are self-selected and lack a denominator.
- Prices/trials/free limits are volatile and locale/channel-specific.
- Official security and product pages are vendor claims, not live verification.
- Most competitor core architectures remain `UNKNOWN`; no inference is presented as a fact.
- Search did not prove absence of a mainstream household-to-household product.
- Current HomePlatform build/tests were not run. Source inspection shows a deterministic constructor mismatch; production/cloud/mobile/frontend/load/offline/realtime are not implemented or verified.
- Legal/privacy implications are risk flags for later professional review, not legal conclusions.

## Final Recommendations

1. Do not lock the masterplan until the Account–Membership ADR question is resolved.
2. Keep the modular monolith and four-project dependency direction.
3. Complete one green, trusted, PostgreSQL-backed `CreateHousehold` slice before expanding.
4. Make MembershipId the durable attribution boundary; allow optional later Account link and several households per Account.
5. Keep authorization roles separate from family relationships and start with a small role matrix.
6. Build tasks/routines, shopping and Today as separate boundaries; keep Household small.
7. Specify recurrence, notification and shared-state semantics before selecting infrastructure.
8. Research calendar integration early but keep OAuth/adapters/sync jobs later.
9. Add activation and export/deletion acceptance criteria to the roadmap.
10. Defer AI, hardware, location, finance, meals, maintenance, legal records and cross-household discovery until core retention and evidence exist.

---

HOMEPLATFORM-COMPETITOR-PRODUCT-ARCHITECTURE-AUDIT-01

STATUS:  
COMPLETE

PRODUCT COVERAGE SCORE:  
2/10

DDD ARCHITECTURE FIT:  
6/10

PORTFOLIO QUALITY:  
4.5/10

TOP 10 FEATURES HOMEPLATFORM SHOULD HAVE:

1. Individual accounts with trusted server-side actor identity.
2. Stable loginless Household membership with later Account linking.
3. Invite, join, remove, leave and ownership-transfer lifecycle.
4. One Account participating safely in several Households.
5. Assigned tasks with completion/reopen/history.
6. Bounded recurring routines with timezone/DST/idempotent occurrence behavior.
7. Fast, trustworthy shared shopping.
8. Authorized Today/agenda read composition.
9. Reminder preferences, quiet hours, deduplication and reliable delivery when needed.
10. Provider-neutral household events plus a later evidence-led calendar import.

TOP 5 OVERLOOKED DOMAIN CONCEPTS:

1. Membership identity independent of credential identity.
2. Optional Account-link lifecycle for a loginless member.
3. Multi-household Account-to-Membership cardinality.
4. Family relationship separate from authorization role.
5. Recurrence occurrence identity and notification preference/delivery semantics.

TOP 5 ARCHITECTURAL PRESSURES:

1. Trusted resource authorization and cross-household isolation.
2. Database uniqueness, last-Owner races and same-resource concurrency.
3. Recurrence, timezone/DST and idempotent occurrence generation.
4. Reliable scheduled notification delivery with preferences/retry/deduplication.
5. External calendar identity, cursor, conflict, revocation and idempotent sync.

TOP 5 FEATURES TO NOT BUILD YET:

1. Location/safety tracking.
2. Autonomous AI ingestion/planning.
3. Meals, expenses and maintenance suites.
4. Hardware, rewards/allowance and legal-record systems.
5. Public/cross-household discovery and generic sharing graph.

CURRENT DOMAIN MODEL:  
MAJOR REWORK

CURRENT MODULAR MONOLITH:  
KEEP

CURRENT SIX-MONTH ROADMAP:  
MODIFY

MOST IMPORTANT ROADMAP CHANGE:  
Resolve and record stable MembershipId + optional AccountId + multi-household + security-only role semantics before the first membership migration.

NEXT DOMAIN QUESTION TO RESOLVE:  
Can a HouseholdMember exist without an Account and later link one atomically without losing identity, assignments, history or permissions?

NEXT FILE TO CREATE AFTER THIS RESEARCH:  
`docs/architecture/adr/0006-separate-account-and-household-membership-identity.md`

SOURCE CODE MODIFIED:  
NO
