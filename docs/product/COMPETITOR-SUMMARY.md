# HomePlatform Competitor Summary

Status: **Decision-relevant research summary, not architecture authority**  
Research cutoff: **2026-08-29**

## Conclusion

The market already contains broad family organizers, mature shared calendars,
task/routine specialists, shopping specialists, co-parenting suites, and wall
displays. HomePlatform should not compete by copying every feature. The most
important research outcome is a modelling correction: reliable household
coordination requires separating authentication Account identity from
Household Membership identity before the first durable schema.

Closest direct references in the audit were FamilyWall, Family Tools, Cozi, and
Jam. TimeTree and Any.do were strong adjacent coordination references. The
evidence is a time-bound product sample, not proof of market share or user
prevalence.

## What changed architecture

- A Household participant may need to exist without an ordinary login.
- Membership needs a stable `MembershipId`; later Account linking must preserve
  assignment and history.
- `AccountId` should be optional on Membership and one Account may participate
  in several Households.
- Scoped uniqueness belongs to a Household; a global Account uniqueness rule
  would block multi-Household participation.
- Owner/Member/Guest are authorization roles. Family relationships are separate
  descriptive concepts.
- Household can own a small Membership collection and last-Owner invariant,
  while Tasks, Routines, Shopping, Events, and Today remain outside that
  aggregate.
- Recurrence, notifications, syncing, and concurrent edits need explicit
  reliability semantics; realtime transport does not solve them.

These conclusions are reflected in the proposed
[ADR 0006](../architecture/adr/0006-separate-account-and-household-membership-identity.md),
[context map](../architecture/target/CONTEXT-MAP.md), and
[domain model](../architecture/target/DOMAIN-MODEL.md). ADR 0006 remains
Proposed and no implementation decision is silently accepted here.

## Product implications

### Required early

- trustworthy Household/Membership identity and authority;
- invitation, activation, leave/remove/transfer lifecycle;
- task assignment and retained history by Membership;
- bounded recurrence semantics;
- shopping and Today as the smallest daily loop;
- export/deletion/shared-record-fate behavior before public beta.

### Research early, implement later

- notification preferences, quiet hours, deduplication, and status semantics;
- calendar provider mapping/provenance/conflict design;
- caregiver capability needs beyond Guest;
- freshness/conflict expectations before realtime.

### Keep deferred

- global Person/Profile matching;
- broad child/caregiver UI;
- external calendar implementation;
- push/realtime platform;
- AI, location, hardware, meals, expenses, maintenance, rewards, and
  cross-Household discovery.

## Credible positioning hypothesis

HomePlatform's strongest potential wedge is a calm, reliable household
coordination loop with explicit responsibility and trustworthy shared state.
That remains a hypothesis to validate through interviews and pilot behavior,
not a proven market advantage.

## Evidence limitations

- competitor behavior, pricing, reviews, and public disclosures change;
- official pages describe claims, not independent production proof;
- review samples reveal failure modes but not population prevalence;
- architecture inferences are constrained by public evidence;
- the research does not establish demand, willingness to pay, or product-market
  fit.

## Supporting evidence

The complete time-bound report, 46-product inventory, 15 detailed profiles,
1,875-row feature matrix, user-pain matrix, architecture evidence, opportunity
map, source register, and validation notebook are retained in the
[competitor-audit archive](../research/archive/competitor-audit-2026-08-29/README.md).

Decision reasoning that remains intentionally prominent:

- [Competitor impact on DDD](../architecture/research/COMPETITOR-IMPACT-ON-DDD.md)
- [Aggregate pressure test](../architecture/research/AGGREGATE-PRESSURE-TEST.md)
