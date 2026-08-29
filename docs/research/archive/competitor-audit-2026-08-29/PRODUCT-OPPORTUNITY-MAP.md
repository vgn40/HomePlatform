# Product opportunity map

> **ARCHIVED SUPPORTING EVIDENCE — NON-AUTHORITATIVE.** Research cutoff:
> 2026-08-29. See the concise
> [competitor summary](../../../product/COMPETITOR-SUMMARY.md).

Research cutoff: **2026-08-29**.

Scores are directional decision aids, not measured market demand. `0` means no six-month fit or feasibility, `1` is low and `5` high. For complexity, a higher number is harder. “Competitor saturation” means feature prevalence in this evidence set, not market share. No opportunity is build-approved without user validation.

| Opportunity | User value | Competitor saturation | Implementation complexity | Architecture complexity | Differentiation potential | Six-month feasibility | Evidence status | Decision |
|---|---:|---:|---:|---:|---:|---:|---|---|
| Trustworthy tasks/routines + Today | 5 | 5 | 3 | 3 | 2 | 5 | strong common denominator | `BUILD CORE` |
| Fast shared shopping with visible freshness | 5 | 5 | 2 | 3 | 2 | 5 | strong specialist/direct evidence; reliability pain | `BUILD CORE` |
| Loginless member with later account link | 5 | 2 | 2 | 3 | 4 | 5 | Family Tools/Jam direct; Skylight profile analogue | `DECIDE PRE-SCHEMA` |
| Multiple households/circles per account | 4 | 3 | 2 | 3 | 4 | 4 | TimeTree/FamilyWall/AppClose/Splitwise | `ALLOW IN MODEL`; UX later |
| Simple household entitlement | 4 | 3 | 2 | 2 | 3 | 4 | per-account/per-parent pricing pain | `RESEARCH COMMERCIAL MODEL` |
| Invite-to-first-value onboarding | 5 | 5 | 2 | 1 | 3 | 5 | adoption friction across products | `BUILD + MEASURE` |
| Notification semantics: preferences, quiet hours and deduplication | 5 | 2 | 3 | 4 | 3 | 3 | reminders are common, but quiet-hours/dedup controls are mostly unknown; repeated missed/duplicate/noisy pain | `SPECIFY SOON`; delivery when needed |
| External calendar design spike | 4 | 5 | 1 | 2 | 1 | 5 | sync is prevalent and failure-prone | `RESEARCH FIRST` |
| One-way external calendar import | 4 | 4 | 4 | 4 | 2 | 2 | widespread expectation; high semantic risk | `JUSTIFIED LATER` |
| Caregiver/grandparent scoped access | 4 | 2 | 3 | 4 | 4 | 2 | Jam/co-parent/multi-circle evidence | `VALIDATE AFTER ROLE MATRIX` |
| Pickup/dropoff/bring-item request flow | 4 | 2 | 3 | 3 | 4 | 2 | Jam/OFW/AppClose | `P2 INTERVIEWS/PILOT` |
| Cross-household event sharing | 3 | 2 | 4 | 5 | 5 | 1 | partial analogues; mainstream gap unproven | `DISCOVERY ONLY` |
| Maintenance assets and service history | 4 | 2 | 4 | 4 | 4 | 1 | Dwellin/HomeZada/Centriq validate distinct language | `P2 SEPARATE CONTEXT` |
| Draft-confirm intake from email/photo/PDF/voice | 4 | 3 | 5 | 4 | 3 | 1 | Jam/Ohai/Skylight pattern | `P3 AFTER RELIABLE MANUAL CORE` |
| Responsive tablet/kiosk Today | 3 | 3 | 3 | 2 | 2 | 2 | wall-display category | `P2 SOFTWARE ONLY` |
| Meal plan → shopping list | 3 | 4 | 4 | 3 | 1 | 1 | saturated; not core | `DEFER` |
| Expenses and settlement | 3 | 4 | 5 | 4 | 1 | 1 | mature specialists/co-parent suites | `DEFER` |
| Rewards/allowance/gamification | 3 | 3 | 4 | 3 | 2 | 1 | child/chore specialist segment | `DO NOT BUILD NOW` |
| Location/safety tracking | 3 | 3 | 5 | 5 | 1 | 0 | high privacy/safety risk and weak thesis fit | `DO NOT BUILD` |
| Public household discovery/profile | 2 | 1 | 5 | 5 | 5 | 0 | whitespace is only a search inference | `DO NOT BUILD; DISCOVERY LATER` |
| Court-grade immutable records | 2 | 2 | 5 | 5 | 1 | 0 | specialist legal-domain requirement | `DO NOT COPY` |
| Hardware product | 2 | 3 | 5 | 5 | 1 | 0 | capital/support/subscription burden | `DO NOT BUILD` |

## Opportunity portfolio

### Core value, not novelty

The strongest six-month opportunity is a better implementation of familiar workflows:

1. a member creates a household;
2. a second person joins in minutes;
3. they assign/complete a recurring responsibility;
4. both see a trustworthy shared shopping state;
5. Today answers “what matters now?” without requiring administration.

The differentiation is trust, flexible membership and low friction—not feature count.

### Most credible differentiated wedge

**Flexible household membership without identity distortion** is the strongest evidence-backed wedge:

- children/dependants can be represented without credentials;
- the same account can participate in multiple households;
- a member can later link a login without losing assignment/history;
- authorization role remains separate from family relationship;
- caregivers can be explored later without remodelling every resource.

This is valuable even if the UI initially exposes only Owner/Member/Guest and one selected household at a time.

### Whitespace hypothesis, not a fact

Multi-circle and co-parent products show partial cross-household coordination. This audit did **not** verify a mainstream generic model for public/private household profiles plus discovery plus shared tasks/shopping between independent households. That is a search result, not proof of market absence or demand. Validate it only after the core product has retention.

## Six experiments before scope expansion

| Experiment | Decision threshold | What it prevents |
|---|---|---|
| 8–12 household interviews with member maps | at least half need a non-account person or multiple household context | speculative Person/ACL model |
| Clickable invite/first-value test | median second-member join and first shared action under five minutes | adoption-killing setup |
| Recurrence concept test | users correctly predict next occurrence/edit-one/edit-series behavior | RRULE complexity with confusing UX |
| Shared-list conflict prototype | participants recover from stale/double edits without silent loss | mistaking SignalR for correctness |
| Calendar-sync problem interviews | repeated high-cost duplicate-entry problem and provider priority | premature OAuth/sync platform |
| Caregiver/pickup workflow interviews | one narrow permission/request pattern repeats across households | generic capability engine |

## Recommended positioning hypothesis

> HomePlatform is the calm, trustworthy daily coordination layer for real households—including people who do not yet have an account and adults who belong to more than one household.

This positioning is a hypothesis. It deliberately avoids competing on location surveillance, hardware, legal records, finance, rewards or autonomous AI.
