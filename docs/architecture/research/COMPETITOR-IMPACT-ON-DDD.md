# Competitor impact on HomePlatform DDD

Status: **Decision input, not architecture authority**  
Research cutoff: **2026-08-29**

**Decision update — 2026-09-20:** earlier recommendations to defer Person are
historical research, superseded by accepted TARGET
[ADR 0007](../adr/0007-person-as-stable-human-identity.md). Person/Relationship
are not implemented; CareCircle is FUTURE. The research below is preserved,
not a competing roadmap.

Historical research boundary, clarified 2026-09-19: “Current”, “next” and
“required soon” below refer to the 2026-08-29 snapshot. ADR 0006 was subsequently
accepted and Membership identity/FK and transfer/leave/close are now implemented.
The older proposed filtered index and blanket history-retention recommendation
are not current policy: the schema uses unfiltered scoped uniqueness; leave
removes membership and future attribution follows the adopted deletion design.
[NEXT-STEPS](../roadmap/NEXT-STEPS.md) alone owns execution order. Feature breadth
in this research is not a delivery commitment.

Current conclusions are consolidated into the
[domain model](../target/DOMAIN-MODEL.md),
[context map](../target/CONTEXT-MAP.md), and accepted
[ADR 0006](../adr/0006-separate-account-and-household-membership-identity.md).
The complete evidence is in the
[competitor-audit archive](../../research/archive/competitor-audit-2026-08-29/README.md).

Working-tree note: after this evidence snapshot, concurrent uncommitted Domain
edits introduced a MembershipId/optional-AccountId prototype. They do not make
the Proposed ADR accepted or provide persistence/concurrency proof.

## Decision summary

- **Current Domain model: MAJOR REWORK of the membership identity concept.** The code volume is tiny, so implementation cost is low; “major” describes the conceptual correction.
- **Current modular monolith: KEEP.** Competitor scope increases the value of modules and explicit use cases, not distributed deployment.
- **Pre-schema minimum:** a stable `MembershipId`, an optional later-linkable `AccountId`, multi-household support and security-only roles.
- **Do not introduce yet:** a global `Person` context, cross-household person matching, arbitrary capability ACLs, Domain Events, an outbox, a scheduler platform or realtime infrastructure.

## Evidence chain

### Facts

1. At the research snapshot, `HouseholdMember` was exactly required nonempty
   `UserId` plus `HouseholdRole`; later uncommitted edits now prototype the
   recommendation.
2. Duplicate membership/link checks remain scoped to one Household instance,
   so multi-household is not prohibited in memory.
3. Family Tools and Jam directly represent children/profiles without ordinary login, while Skylight profiles are an adjacent analogue [S011, S016, S034]. FamilyWall's no-email child still receives login/password [S088]; Google separately demonstrates supervised child Accounts [S054].
4. TimeTree, FamilyWall, AppClose and Splitwise support several calendars/circles/groups [S007, S020, S043, S048].
5. The earlier planning baseline already distinguished Parent/Child descriptions from Owner/Member/Guest authorization; the current [domain model](../target/DOMAIN-MODEL.md) now owns that language.

### Inference

If assignment and history reference only an authentication UserId, HomePlatform cannot represent a child before login and later attach their account without changing identity or creating a duplicate member. A membership therefore needs identity independent of credentials.

The evidence does **not** prove that HomePlatform needs one global `Person` shared across households. That model would create premature cross-household matching, ownership and privacy problems.

### Recommendation

Use the following conceptual minimum as the next domain question, not as approved code:

```text
Identity & Access
  AccountId (credential-bearing, trusted authentication subject)
             |
             | optional link
             v
Households
  HouseholdMember
    MembershipId (stable)
    HouseholdId
    AccountId?       (nullable; later linkable)
    Role             (Owner / Member / Guest; authorization only)
    Lifecycle        (active/left/removed as product semantics require)
```

Assignments and historical attribution should point to `MembershipId`. A global `Person`/Profile is added only when a proven cross-household profile lifecycle needs it.

## Ubiquitous-language corrections

| Term | Use | Must not mean |
|---|---|---|
| Account | credentials, sign-in, security/session lifecycle | a person in every household |
| Household | one protected collaboration boundary | an authentication tenant or global family graph |
| Membership | one represented participant inside one Household | an Identity user or family relationship |
| Member role | authorization level within one Household | Parent, Child, Partner or Grandparent |
| Relationship | optional later descriptive family/care relationship | automatic authority |
| Invitation | expiring offer to link/create a Membership | permanent access grant |
| Assignment | responsibility attributed to a Membership | ownership of the authenticated account |
| Household context | HouseholdId used for resource authorization | server-side global “active household” state |

## Domain questions that must be answered before schema hardens

1. May a HouseholdMember exist without a login? **Recommended default: yes.**
2. Does Membership have a stable ID separate from AccountId? **Recommended default: yes.**
3. Can an Account be linked later without losing tasks, history or permissions? **Recommended default: yes, as an explicit verified use case.**
4. Can one Account belong to several Households? **Recommended default: yes; no global unique AccountId constraint.**
5. What makes two account-linked memberships duplicates? **Candidate:** a filtered unique `(HouseholdId, AccountId)` where AccountId is present.
6. What identifies a loginless member before linking? **Open:** MembershipId plus household-owned display data; avoid invented UserId.
7. Who may link an Account to a membership and how is identity verified? **Open security decision.**
8. Does leaving retain historical attribution while revoking current access? **Recommended default: yes.**
9. Can the last Owner leave, be removed or demoted? **Recommended default: no without atomic transfer.**
10. Does every relationship need persistence now? **Recommended default: no.**

## Business rules that justify DDD constructs

| Construct | Real rule/workflow | Timing |
|---|---|---|
| Household aggregate | a household must retain an active Owner; membership mutation is authorized and consistent | required soon |
| Membership entity/identity | loginless member can later link an account while identity/history survives | required now, pre-schema |
| Invitation aggregate/lifecycle | offer can expire, be revoked and be accepted once | required soon |
| RecurrencePattern value object | daily/weekday local-time rules and edit semantics must remain valid | required soon |
| Routine aggregate | activation/deactivation and occurrence identity are independent of one task instance | required soon |
| ShoppingList aggregate | item state belongs to a list; one initial active-list rule | required soon |
| HouseholdEvent aggregate | event time/audience/state changes independently | later in six months if capacity |
| Today read model | composes authorized facts without cross-context writes | required soon; not aggregate |
| Calendar anti-corruption layer | provider IDs/cursors/conflicts must not leak into internal Event language | justified later |
| Notification delivery records | retry/dedup/preferences exist when reminders deliver outside requests | required with first reliable delivery |
| Domain Events | multiple independent reactions to one committed business fact | not justified now |

## Household aggregate boundary

Keeping a small membership collection inside Household is defensible for the beta because family-sized membership is small and the last-Owner invariant is aggregate-wide. However:

- list/read queries must not load Household plus all tasks/events/shopping;
- assignments store MembershipId references, not child entities inside Household;
- database constraints/concurrency must prove owner and uniqueness races;
- expose a non-downcastable read-only member view;
- add an exit criterion if membership writes become contended or the model grows into a network graph.

A separate Membership aggregate is not required merely because Membership has an ID. Revisit the boundary if independent membership lifecycle, large networks, capability overrides or cross-household person linking make whole-collection loading/contention material.

## Context implications

The smallest defensible initial context map is:

```text
Identity & Access -> trusted AccountId
                    |
                    v
Households -------> authorization facts by HouseholdId/MembershipId
     |                 |              |
     v                 v              v
Tasks & Routines    Shopping       Events
       \               |             /
        +---------- Today read -------+

Explicit application orchestration -> email/push/calendar adapters when triggered
```

“People/Profile,” “Notifications” and “Calendar Integration” should become independent bounded contexts only when their language, lifecycle and invariants demonstrably diverge. They may begin as clear modules/ports without new projects or services.

## Product discoveries that do not justify new Domain code now

- AI ingestion: first define provenance and review/confirm UX; integration concern until accepted action becomes an explicit use case.
- Realtime: transport does not create a domain concept and does not prevent lost updates.
- Hardware: presentation channel only.
- Location: separate high-risk product area; no core-domain fields.
- Meals, expenses and maintenance: later bounded languages, not extensions to HouseholdTask.
- Cross-household discovery: unvalidated opportunity, not a reason for a social graph today.
- Legal immutability: co-parenting specialist rule, not ordinary household default.

## Portfolio DDD consequence

The strongest portfolio story is not the number of patterns. It is the ability to explain:

1. why Account and Membership have different identity/lifecycles;
2. why relationship does not grant authority;
3. why Household owns the last-Owner invariant but not tasks, lists or events;
4. why recurrence uses explicit time semantics and idempotent occurrence identity;
5. why Today is a read composition;
6. why adapters/outbox/realtime are introduced only when a delivery/integration requirement triggers them.

That story is compatible with the existing four projects and a modular monolith.
