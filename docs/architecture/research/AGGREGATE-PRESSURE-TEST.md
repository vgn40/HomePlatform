# Aggregate and architecture pressure test

Status: **Decision-relevant research, not architecture authority**  
Research cutoff: **2026-08-29**

**Decision update — 2026-09-20:** earlier recommendations to defer Person are
historical research, superseded by accepted TARGET
[ADR 0007](../adr/0007-person-as-stable-human-identity.md). Person/Relationship
are not implemented; CareCircle is FUTURE. The research below is preserved,
not a competing roadmap.

Historical research boundary, clarified 2026-09-19: all “Current support” and
phase recommendations below describe the 2026-08-29 snapshot. Membership
identity, nullable Account FK and sequential transfer/leave/close are now
implemented. Future feature examples do not adopt retention policies or schedule
features. [NEXT-STEPS](../roadmap/NEXT-STEPS.md) alone owns execution order;
[DELETION-DESIGN](../../privacy/DELETION-DESIGN.md) owns lifecycle decisions.

The current summary lives in the
[domain model](../target/DOMAIN-MODEL.md). Raw supporting research is in the
[competitor-audit archive](../../research/archive/competitor-audit-2026-08-29/README.md).

These are proposed consistency boundaries, not code instructions. “Current
support” statements below refer to the research snapshot before later
uncommitted Account/Membership prototype edits; use the Domain Model for the
live description.

## Aggregate candidates

### Household

**ROOT:** `Household`

**ENTITIES:** `HouseholdMember` while household-sized membership and the last-Owner invariant remain manageable.

**VALUE OBJECTS:** bounded `HouseholdName`; perhaps `MembershipId` as strongly typed identity. Do not add wrappers without error-prevention value.

**INVARIANTS:** nonblank/bounded name; one stable membership identity per member; at most one account-linked membership for `(HouseholdId, AccountId)`; at least one active Owner; role changes/removal are authorized; optional account link is unique and verified.

**TRANSACTIONAL BOUNDARY:** create/rename; add/link member; change role; leave/remove; transfer ownership. Use an optimistic version or equivalent conditional mutation where owner races exist, plus database uniqueness.

**WHY THIS IS ONE AGGREGATE:** last-Owner and duplicate-membership decisions need one current membership set; household size is naturally small for the beta.

**WHAT MUST NOT BE INCLUDED:** tasks, routines, shopping lists, events, notification history, calendar-provider objects, credentials or Identity entities.

**EXIT CRITERIA:** split membership writes from Household only if independent lifecycle/capabilities or contention makes loading/updating the small set materially harmful. A stable MembershipId does not by itself require a separate aggregate.

### HouseholdMember, User and Account boundary

**`HouseholdMember`: ENTITY, NOT AGGREGATE ROOT NOW.** It has a stable `MembershipId` and lifecycle inside one Household, but the current last-Owner, duplicate-link and membership-transition invariants are household-wide. Mutate it through `Household` while that small consistency boundary remains workable. A stable identity does not by itself justify a repository or separate aggregate.

**`User`: NOT A JUSTIFIED PRODUCT-DOMAIN AGGREGATE.** The current `User` type is only a credential-shaped identity placeholder and has no demonstrated behavior or independent invariant. Do not grow it into a global person graph. Retire or rename that concept when Identity is introduced; create a separate profile concept only if real profile behavior later needs ownership.

**`Account`: IDENTITY BOUNDARY, NOT A HOUSEHOLDS AGGREGATE.** Authentication credentials, MFA, lockout and account lifecycle belong to the Identity framework/store and are referenced from the product domain through a trusted `AccountId`/current-actor abstraction. A Household membership may have no Account initially and may later link exactly one verified Account without changing `MembershipId` or history.

**BOUNDARY CONSEQUENCE:** `HouseholdMember` models participation in one Household; `Account` models an authenticated principal; relationship labels model family/caregiving meaning; roles/capabilities model authority. None is interchangeable, and no new global `Person` aggregate is justified before a concrete cross-household invariant appears.

### Invitation

**ROOT:** `Invitation`

**ENTITIES:** none initially.

**VALUE OBJECTS:** hashed invite token, expiry, invitee email/address if used.

**INVARIANTS:** belongs to one Household; can be accepted once; cannot be accepted after expiry/revocation; acceptor is eligible; acceptance creates/links at most one Membership.

**TRANSACTIONAL BOUNDARY:** issue/revoke/accept. Acceptance may use explicit Application orchestration with Household and one database transaction; do not make Invitation a child merely to force cross-lifecycle loading.

**WHY THIS IS ONE AGGREGATE:** invitation status has an independent expiry/retry/revocation lifecycle.

**WHAT MUST NOT BE INCLUDED:** Household members collection, email provider payloads, authentication tokens.

### HouseholdTask

**ROOT:** `HouseholdTask`

**ENTITIES:** none initially.

**VALUE OBJECTS:** bounded title, due time with timezone semantics if applicable, TaskStatus.

**INVARIANTS:** one Household; assignee references an eligible Membership; legal completion/reopen/archive transitions; completion attribution/time is internally consistent.

**TRANSACTIONAL BOUNDARY:** create/update/assign/complete/reopen/archive one task.

**WHY THIS IS ONE AGGREGATE:** the task lifecycle and concurrency are independent from Household and other tasks.

**WHAT MUST NOT BE INCLUDED:** whole Household/member objects, recurrence series, shopping items, notification delivery records.

### Routine

**ROOT:** `Routine`

**ENTITIES:** occurrence only if materialized behavior/history requires an entity; otherwise occurrence identity can be a separate Task/Completion record.

**VALUE OBJECTS:** `RecurrencePattern`, local time, IANA timezone, occurrence key/local date.

**INVARIANTS:** supported recurrence is valid; activation range is valid; exactly one logical occurrence per routine/key; pause/edit applies with explicit effective semantics.

**TRANSACTIONAL BOUNDARY:** create/edit/activate/pause Routine; materialize/complete one occurrence idempotently.

**WHY THIS IS ONE AGGREGATE:** series rules change together and have an independent lifecycle from a single task occurrence.

**WHAT MUST NOT BE INCLUDED:** general scheduler, arbitrary RRULE parser, provider job state, all future instances pre-generated without need.

### ShoppingList

**ROOT:** `ShoppingList`

**ENTITIES:** `ShoppingItem` with stable ID.

**VALUE OBJECTS:** bounded item text/quantity if semantics justify them; list status.

**INVARIANTS:** one Household owner; valid item state; initially at most one active list if retained; same-item transitions do not silently overwrite.

**TRANSACTIONAL BOUNDARY:** create/archive list; add/check/uncheck/remove one item. Commands target item IDs; unrelated item edits should not be forced to conflict through a coarse list version unless measurement proves that boundary acceptable.

**WHY THIS IS ONE AGGREGATE:** list owns item membership and initial active-list policy.

**WHAT MUST NOT BE INCLUDED:** product catalog, offers, recipes, meal plans, Household members, realtime connections.

### HouseholdEvent

**ROOT:** `HouseholdEvent`

**ENTITIES:** none initially; attendees/audience may be value/reference records if needed.

**VALUE OBJECTS:** event interval, local timezone, bounded title.

**INVARIANTS:** valid start/end; one owning Household; authorized audience; legal cancellation/update.

**TRANSACTIONAL BOUNDARY:** create/update/cancel one internal event.

**WHY THIS IS ONE AGGREGATE:** an event changes independently and does not require a whole calendar/household transaction.

**WHAT MUST NOT BE INCLUDED:** Google/Apple/Outlook tokens, sync cursor, external tombstone, all attendees as aggregate roots, Today projection.

### ExternalCalendarConnection — justified later

**ROOT:** provider connection or sync subscription, only when the first provider is approved.

**ENTITIES:** calendar mapping/subscription as needed.

**VALUE OBJECTS:** provider type, external calendar/event identity, cursor, sync status.

**INVARIANTS:** connection belongs to one authorized Account/Household scope; provider ID maps deterministically; cursor advancement and deletion are idempotent; revoked credentials stop import.

**TRANSACTIONAL BOUNDARY:** connect/revoke; consume one provider change page/upsert batch according to explicit cursor semantics.

**WHY THIS IS ONE AGGREGATE:** provider lifecycle and failure/retry state differ from an internal Event.

**WHAT MUST NOT BE INCLUDED:** raw provider SDK objects in Domain, unrelated providers in one transaction, Household aggregate.

### Today

**ROOT:** none.

**ENTITIES / VALUE OBJECTS:** none on the write side.

**INVARIANTS:** authorized and deterministic for the requested Household/local date; read causes no writes.

**TRANSACTIONAL BOUNDARY:** no cross-module transaction; a no-tracking read composition.

**WHY THIS IS NOT AN AGGREGATE:** it protects no write invariant and exists to compose several contexts.

**WHAT MUST NOT BE INCLUDED:** materialized Today table/cache until profiling proves need.

## Twelve future scenarios

### Scenario A — 2 adults, 3 children, 2 grandparents

- **Current support:** only by inventing seven UserIds.
- **Breaks:** child/non-account identity, later linking, relationship and restricted access.
- **Owner:** Households; Identity integration only for linked accounts.
- **Invariant:** unique Membership identity; at least one Owner; relationship never grants authority automatically.
- **Transaction/concurrency:** membership mutation and last-Owner operations.
- **Evolution:** stable MembershipId + optional AccountId is required pre-schema. Global Person and capability engine are not.

### Scenario B — one Account in own, partner's and parents' households

- **Current support:** structurally possible because duplicate check is household-local; no product/API support.
- **Breaks:** persistence/list queries, authorization and client context selection.
- **Owner:** Households.
- **Invariant:** filtered uniqueness per `(HouseholdId, AccountId)`, never global uniqueness.
- **Transaction/concurrency:** ordinary per-household mutation and database constraint.
- **Evolution:** schema support now; switching UI/server preference later.

### Scenario C — 10,000 households concurrently edit shopping lists

- **Current support:** none.
- **Breaks:** no list model, load proof, atomic commands or conflict semantics.
- **Owner:** Shopping.
- **Invariant:** active-list uniqueness and valid item state; no lost same-item update.
- **Transaction/concurrency:** unique constraint plus item-targeted conditional/atomic updates.
- **Evolution:** PostgreSQL/modular monolith remain plausible; load test before cache/realtime.

### Scenario D — recurring tasks generate future work

- **Current support:** planned only.
- **Breaks:** timezone/DST, missed occurrences, edit-one/series, retries.
- **Owner:** Tasks & Routines.
- **Invariant:** one occurrence per Routine/occurrence key.
- **Transaction/concurrency:** unique key and idempotent materialization/completion.
- **Evolution:** worker only when generation/reminder must happen without a request.

### Scenario E — calendar sync imports external events

- **Current support:** explicitly deferred.
- **Breaks:** provider connection, external identity, cursor, tombstone, conflict, consent.
- **Owner:** later Calendar Integration; internal Events stays provider-neutral.
- **Invariant:** deterministic mapping, no cross-household leakage, idempotent upsert.
- **Transaction/concurrency:** cursor/retry deduplication and conditional updates.
- **Evolution:** design spike earlier; implementation and adapter later.

### Scenario F — push notifications scheduled reliably

- **Current support:** none.
- **Breaks:** preferences, device/channel, quiet hours, logical deduplication, attempt/status/retry.
- **Owner:** Application/supporting Notifications module; business context owns reminder intent.
- **Invariant:** authorized recipient, preference respected, one logical notification despite retry.
- **Transaction/concurrency:** scheduled job claim; later same-database outbox if commit/delivery handoff must be crash-safe.
- **Evolution:** no broker or generic event bus initially.

### Scenario G — grandparent gets restricted permissions

- **Current support:** generic Guest planned as read-only; no enforcement.
- **Breaks:** “calendar/pickup but not expenses/private notes” may exceed fixed role.
- **Owner:** Households authorization policies; resource contexts enforce.
- **Invariant:** relationship does not imply permission.
- **Transaction/concurrency:** role/capability change sees current authorization/version.
- **Evolution:** start with role matrix; add narrow capability only after a repeated workflow.

### Scenario H — user leaves a household

- **Current support:** no leave/remove lifecycle.
- **Breaks:** access revocation, history attribution and last-owner rule.
- **Owner:** Households.
- **Invariant:** current access ends; historical facts remain attributed to MembershipId; an Owner remains.
- **Transaction/concurrency:** atomic membership transition with version/guard.
- **Evolution:** required Phase 3 use case.

### Scenario I — last Owner attempts to leave

- **Current support:** initial owner only; no mutation rule.
- **Breaks:** household can become unmanaged.
- **Owner:** Household aggregate.
- **Invariant:** at least one active Owner.
- **Transaction/concurrency:** mandatory race proof for simultaneous leave/demote/transfer.
- **Evolution:** version/conditional update plus PostgreSQL test.

### Scenario J — two members edit the same task

- **Current support:** no Task model.
- **Breaks:** undefined conflict/last-write behavior.
- **Owner:** HouseholdTask.
- **Invariant:** legal transitions; no silent destructive overwrite.
- **Transaction/concurrency:** conditional version/state transition or atomic command; idempotent completion for retries.
- **Evolution:** define user-visible conflict first. SignalR does not solve it.

### Scenario K — Household A shares an event with Household B

- **Current support:** none; connections deferred.
- **Breaks:** single Household ownership cannot express grant/revocation/audience.
- **Owner:** Events plus a later Household Connections/Sharing capability.
- **Invariant:** source Household retains ownership; explicit scope; revocation closes access.
- **Transaction/concurrency:** idempotent grant/revoke with authorization.
- **Evolution:** separate access-grant concept if validated; do not merge households or duplicate whole aggregates.

### Scenario L — child later receives login

- **Current support:** semantically fails because the child already must have a UserId.
- **Breaks:** replacement or duplicate membership loses continuity.
- **Owner:** Households with Identity link verification.
- **Invariant:** Account links to at most one active membership in that Household; MembershipId/history/role survive.
- **Transaction/concurrency:** verified atomic link with filtered uniqueness.
- **Evolution:** strongest pre-schema requirement; no global Person required.

## Does Household always load all members?

**Beta answer:** membership mutation may load the small member set to defend last-Owner and duplicate rules. Ordinary reads, task/list/event mutations and authorization lookups should use focused queries/projections and never load the Household aggregate graph unnecessarily.

**Revisit when:** a household/network can contain materially many members, capability updates are frequent, independent Membership lifecycle dominates, or optimistic conflicts appear. Until then, splitting the aggregate would trade one small collection for cross-aggregate owner consistency without evidence.

## Pressure-test verdict

The modular monolith remains a suitable target across all twelve paper scenarios, but runtime and load behavior remain unverified. The current membership identity does not survive A or L and only structurally hints at B. Recurrence, calendar sync, notifications and cross-household sharing require later modules/ports and explicit idempotency, but none currently justify microservices, a broker, Redis, event sourcing or SignalR.
