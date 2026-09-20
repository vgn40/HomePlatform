# ADR 0007: Person as Stable Human Identity

Status: **Accepted TARGET; not implemented**
Accepted: **2026-09-20**

Extends [ADR 0006](0006-separate-account-and-household-membership-identity.md).
Supersedes its deferral of Person and direct Account-to-Membership target, not
its historical rationale or the separation of credentials and participation.
ADR 0006 remains unchanged as the historical decision.

## Context

HomePlatform helps people coordinate shared life across households, relationships
and changing family structures. The model must evolve to support couples,
households without children, families with children, blended families, separated
parents, grandparents and other caregivers without requiring a different product.

AS-IS at committed `main@7162c35` is Infrastructure-owned ASP.NET Core Identity
and HouseholdMember with stable MembershipId, nullable AccountId and
Owner/Member/Guest roles. Household creation/transfer/leave/close and persistence
exist; HTTP routes are Testing-only. Nonmember concealment is committed in
`7162c35`; DeleteAccount remains separate, uncommitted and not declared complete.
Person, Relationship and CareCircle
are absent from source. Acceptance of this ADR changes documentation, not code.

ADR 0006 separated credentials from participation. That remains useful but
cannot identify one loginless child across two Households: Membership is local
to participation, while the human exists independently of it. After parental
separation, Emma must be representable as one Person with distinct memberships
in her mother's and father's Households, where product rules permit.

## Decision

1. **Account** is authentication/credentials. ASP.NET Core Identity remains in
   Infrastructure. Do not create a Domain Account aggregate to mirror it.
2. **Person** is the stable human identity and may optionally have an Account.
   An adult user has both, a young child needs no Account, and a grandparent's
   Account is optional. Exact link/persistence/verification is not decided here.
3. **HouseholdMembership** is a Person's participation in a Household, with its
   own identity and contextual authority. Household remains an important
   aggregate/context, not the identity of the entire family. A Person may have
   multiple memberships where product rules permit, without duplicate Persons
   solely because they participate in different Households.
4. **Relationship** connects Persons independently of Household membership.
   Co-residence and family/social relationship are separate. Parent-of,
   partner-of and grandparent-of are examples, not a finalized taxonomy.
5. Child, parent, partner, grandparent and step-parent are relational/contextual
   roles, not CLR/entity subtypes. Do not create `Child : Person` or similar
   inheritance. Family relationships never grant permissions implicitly.
6. **CareCircle / CareCircleMembership** is accepted only as a FUTURE concept
   for people caring for a child across Household boundaries. Parents,
   step-parents, grandparents, babysitters and trusted caregivers need not
   co-reside. Possible schedule/pickup/selected-information/temporary access is
   illustrative; no immediate implementation, medical or custody design follows.

```text
AS-IS:  Account -- optional link -- HouseholdMember -- Household

TARGET: Account -- optional link -- Person -- Relationship -- Person
                                     |
                              HouseholdMembership
                                     |
                                  Household

FUTURE: Person (child) -- CareCircle -- CareCircleMembership -- Person (caregiver)
```

## Why these identities differ

Account answers who can authenticate; it cannot represent a person without
credentials. Membership answers how someone participates in one Household;
its creation/removal does not define the existence of the human. Person gives
that human continuity across changing contexts. A family relationship can
continue without co-residence, and care coordination can span Households.
Neither identity continuity nor a relationship grants indefinite data retention
or access to another context.

## Incremental migration implications

[NEXT-STEPS](../roadmap/NEXT-STEPS.md) alone orders execution. First design the
smallest Person foundation and Account link; then introduce it and evolve
membership through bounded changes, followed by a concrete flow that proves
value without login. Add Relationship only when a real flow requires it;
CareCircle follows later concrete needs. Do not replace the architecture wholesale.

Preserve MembershipIds where participation continues. Map current Account-linked
and loginless rows explicitly; inspect actual data before selecting backfill,
compatibility, constraints and rollback. Current loginless members lack enough
data to establish sameness across Households. Never infer identity by guessing
or silently merge records. No schema or migration algorithm is adopted here.

Existing resource authorization and Owner account eligibility must remain
correct through the transition. The current Account-linked DeleteAccount
policy remains applicable to AS-IS; future Account deletion versus Person fate
must be designed explicitly before affected changes. Do not duplicate or overwrite
the current uncommitted lifecycle work.

## Consequences

The model can represent children without logins, distinguish people from
participation, and retain one human identity across Household changes. This
makes the .NET/backend portfolio explainable through real domain choices.

Costs include another identity boundary, linking/management authorization,
migration/backfill and explicit cross-context privacy/lifecycle rules. Person
must remain minimal; this decision does not authorize an arbitrary social graph,
profile platform, automatic matching or a generic permission engine.

Product/domain development leads. Immediate current-Account checks, Household
optimistic concurrency, advanced queries and broader operational hardening are
preserved in the [debt register](../roadmap/TECHNICAL-DEBT-REGISTER.md) and
[security roadmap](../roadmap/SECURITY-ROADMAP.md), to be pulled forward for
concrete risk, feature dependency or release. Deferral does not establish
production readiness or waive a new flow's basic correctness/authorization.

## Deliberately undecided

- Minimum Person data, management authority and aggregate/module boundary.
- Exact optional Account–Person persistence/linking, verification and unlinking.
- Mapping existing data, duplicate resolution, compatibility and rollback.
- Person lifecycle and Account deletion versus Person survival, retention,
  export and cross-Household access; no implicit deletion or retention cascade.
- Relationship directionality, symmetry, lifecycle, effective dates, permissions
  and taxonomy.
- Detailed CareCircle structure/permissions and child-account policy; medical,
  custody and detailed privacy capabilities are not designed now.
- Playdates, temporary coordination and trusted sharing between Households are
  exploration only. `HouseholdRelationship` is not an accepted entity, schema
  or roadmap implementation item; Person relationships or temporary shared
  contexts may be preferable.

Keep the modular monolith. No microservices, event sourcing, generic repository
or mediator framework, broker, graph database or generic permission engine is
justified by this decision.
