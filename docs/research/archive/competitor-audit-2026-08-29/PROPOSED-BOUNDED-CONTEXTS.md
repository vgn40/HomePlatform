# Proposed bounded contexts

> **ARCHIVED RESEARCH INPUT — NON-AUTHORITATIVE.** The proposal was
> consolidated into the current
> [CONTEXT-MAP.md](../../../architecture/target/CONTEXT-MAP.md).

Research cutoff: **2026-08-29**. These are conceptual language/ownership boundaries for a modular monolith, not proposed microservices or necessarily separate .NET projects.

## Context map

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
                  membership  |    |    | authorization facts
                  references  |    |    |
                 +------------+    |    +----------------+
                 v                 v                     v
        +--------+---------+ +-----+------+      +-------+------+
        | Tasks & Routines | |  Shopping  |      |    Events    |
        +--------+---------+ +-----+------+      +-------+------+
                 \                 |                     /
                  +----------------+--------------------+
                                   v
                          +--------+--------+
                          | Today / Agenda  |
                          | read composition|
                          +-----------------+

Application orchestration/ports ---> Email / Push / Calendar providers
                                  (supporting modules first; contexts later if justified)
```

## Initial contexts

### Identity & Access

**Language:** Account, credential, authentication subject, email confirmation, session/token, lockout, recovery, device session.

**Owns:** credential-bearing account and authentication lifecycle. ASP.NET Core Identity remains Infrastructure-owned; Application receives a trusted `AccountId` through a narrow current-user port.

**Invariants:** credential rules, normalized unique email decision, verified authentication, no caller-selected actor.

**Lifecycle:** register, confirm, sign in, refresh, reset, sign out/revoke, delete/disable.

**Consistency:** Identity store transaction. It does not atomically own Household membership.

**Integration:** publishes/returns AccountId to explicit Application use cases; Households verifies linkability through a port/use case. No global Identity roles for household authority.

### Households

**Language:** Household, Membership, Owner, Member, Guest, Invitation, join, leave, remove, transfer, link account.

**Owns:** collaboration boundary, member identity within the household, invitations and resource-authorization facts.

**Invariants:** valid name; stable MembershipId; optional Account link; no duplicate account-linked membership in one household; at least one active Owner; only authorized operations mutate membership; invite expiry/single acceptance.

**Lifecycle:** create, rename, invite, accept, add loginless member, link account, change role, leave/remove, transfer ownership, archive/delete.

**Consistency:** one Household transaction for membership/owner invariants while household-sized membership remains small. Database uniqueness and concurrency are mandatory evidence.

**Integration:** receives trusted AccountId from Identity; supplies MembershipId/role/access facts to other contexts. Relationship labels, if added, never automatically grant authority.

### Tasks & Routines

**Language:** HouseholdTask, assignee, due time, complete, reopen, Routine, RecurrencePattern, occurrence, skip, series.

**Owns:** responsibilities, completion attribution and limited recurrence behavior.

**Invariants:** task belongs to one Household; assignee is an eligible Membership; legal status transitions; recurrence is valid in an IANA timezone; one logical occurrence per routine/local occurrence key.

**Lifecycle:** create/update/assign/complete/reopen/archive task; create/activate/pause/edit routine; derive/materialize/complete occurrence.

**Consistency:** one Task or Routine aggregate per mutation; unique occurrence key handles retries. Household is not loaded with all tasks.

**Integration:** queries Households authorization/eligibility; exposes due/overdue data to Today; sends explicit reminder intent through Application when notifications exist.

### Shopping

**Language:** ShoppingList, ShoppingItem, add, check, uncheck, remove, archive, active list.

**Owns:** list and item state. It does not own product catalog, meal recipes or household membership.

**Invariants:** item belongs to one list/household; legal item transition; initially at most one active list if that scope is retained; no silent lost update for same item.

**Lifecycle:** create/activate/archive list; add/check/uncheck/remove item.

**Consistency:** list-level transaction, but commands should target stable item IDs so unrelated item changes need not collide on one coarse version.

**Integration:** verifies membership authorization; exposes summary to Today. Meal-to-list integration is later and explicit.

### Events

**Language:** HouseholdEvent, title, start/end, local timezone, attendee/audience, cancel.

**Owns:** internal household event. It does not own external provider accounts, cursors or sync conflicts.

**Invariants:** valid interval/timezone; one Household owner; authorized audience/change.

**Lifecycle:** create/update/cancel/archive.

**Consistency:** one event per mutation; no transaction with every attendee/resource.

**Integration:** contributes to Today. A future Calendar Integration module maps external events without contaminating internal language.

### Today / Agenda

**Language:** today, due, overdue, upcoming, shopping summary, household local date.

**Owns:** no write-side business object. It owns an Application query contract and DTO only.

**Invariants:** authorized, deterministic view for household/local date; reads cause no writes.

**Lifecycle/consistency:** none beyond query versioning/freshness. No Today aggregate/table by default.

**Integration:** reads task/routine, event and shopping projections through focused query ports.

## Supporting modules that are not independent contexts yet

### Notifications / Delivery

Start with documented reminder/preference/quiet-hours semantics and explicit Application orchestration. Introduce a scheduled-delivery table/worker only when delivery must occur without a request. A same-database outbox becomes justified only when a committed business fact must atomically produce durable delivery work. No broker is justified.

Promotion trigger to a context: independent preference/device/delivery lifecycle, multiple channels/providers, retries/status and business ownership that no feature module can coherently own.

### Calendar Integration

Start with a research/design spike. Future adapter/anti-corruption language includes ProviderConnection, ExternalCalendarId, ExternalEventId, SyncCursor, Tombstone, SyncStatus and conflict policy.

Promotion trigger: first approved provider and a lifecycle independent from internal events.

### People / Profile

Do not create a global Person context for the P0 membership fix. Household-owned display identity plus optional AccountId is enough until one profile must legitimately survive/share across households.

Promotion trigger: a cross-household person profile, verified linking/matching, independent consent/ownership and real profile behaviors.

## Later candidate contexts

| Candidate | Evidence | Distinct language/invariants | Decision |
|---|---|---|---|
| Meals | AnyList, FamilyWall, Cozi, Paprika, Mealime | recipe, serving, dietary preference, meal slot, ingredient | P2; separate from Shopping/Task |
| Home Maintenance | Dwellin, HomeZada, Centriq | asset, manual, warranty, service interval/history | P2; strong separate-language case |
| Expenses | Splitwise and co-parenting suites | payer, split, balance, settlement, shared-record ownership | P2; sensitive, separate context |
| Care Logistics | Jam, OFW, AppClose, CareCalendar | pickup/dropoff request, driver, bring item, accept/decline | discovery/P2 |
| Household Connections | multi-circle/co-parent analogues | link/grant, source household, recipient household, revoke | discovery only; high cross-boundary risk |
| Location/Safety | FamilyWall, Life360, co-parent check-ins | consent, precise fix, retention, emergency disclaimer | P3/not justified |
| Rewards/Allowance | Sweepy, Nipto, S'moresUp, BusyKid | points, approval, payout, reward economy | P3 specialist |

## Integration relationships

| Upstream | Downstream | Relationship | Contract |
|---|---|---|---|
| Identity & Access | Households | customer/supplier | trusted AccountId; account-link verification |
| Households | Tasks/Shopping/Events | conformist to stable access contract | `CanAccess`/Membership reference through explicit use-case/query boundary |
| Tasks/Shopping/Events | Today | open host/read contract | stable read DTO/projection, no domain-object leakage |
| Feature contexts | Notifications | published intent/application orchestration later | recipient + logical notification ID; preferences applied downstream |
| External calendar provider | Calendar Integration | anti-corruption layer | provider DTO/IDs/cursors translated into internal integration model |
| Calendar Integration | Events/Today | explicit mapping/query | no provider object inside Domain Event aggregate |

## Boundary rules

1. Do not create one project per context during the six-month beta.
2. Do not reference Domain/EF/Identity entities across HTTP or module contracts.
3. Use IDs and explicit ports; avoid shared “Common” business abstractions.
4. Do not place HouseholdTask, ShoppingList or HouseholdEvent inside the Household aggregate.
5. Keep one use-case commit unless a proven workflow requires coordinated aggregates.
6. Context boundaries explain vocabulary and ownership; they do not justify network calls.

## Validation gates

- Membership schema is blocked until loginless member, stable ID, Account link and multi-household decisions are recorded.
- Task/Routine schema is blocked until local-time/DST/edit-series semantics are accepted.
- Event schema is blocked until internal time semantics are accepted; provider sync implementation remains later.
- Notification infrastructure is blocked until the first concrete delivery/reliability requirement.
- Cross-household context is blocked until interviews/pilot evidence establishes repeated demand and access semantics.
