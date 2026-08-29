# ADR 0006: Separate Account and Household Membership Identity

Status: Proposed

## Context

The proposal baseline used a required `UserId` as `HouseholdMember` identity.
That made an authentication identity carry two meanings: the Account
that can sign in and the membership through which someone participates in one
Household.

Those concepts have different lifecycles. A child, grandparent, caregiver, or
other household participant may need to exist without an Account. A loginless
member may later gain an Account, and that link must not replace the identity
used by assignments or historical attribution. Conversely, one Account may
participate in several Households, with a distinct role and membership lifecycle
in each.

Using `UserId` as the durable membership identity cannot represent these cases
without invented Accounts, replaced identifiers, or ambiguous history. It also
encourages authorization code to confuse successful authentication with access
to a particular Household. Household access must depend on a current membership
and its authority role, not on possession of an Account identifier or a family
relationship label.

This decision is required before the first durable membership mapping and
migration. The modular monolith and its Domain/Application/Infrastructure/API
layering remain unchanged.

### Working-tree update

After this proposal was drafted, concurrent uncommitted Domain edits introduced
`MembershipId`, optional `AccountId`, and AccountId-based Household creation.
The handler and tests still use prior signatures; no fresh green build, mapping,
migration, linking workflow, or concurrency proof exists. That prototype does
not change this ADR's Proposed status or approve its unresolved consequences.

## Decision

1. `Account` and `HouseholdMember` have independent identities.
2. Each `HouseholdMember` owns a stable, non-empty `MembershipId`.
3. A `HouseholdMember` may exist without an Account; its `AccountId` is optional.
4. Linking or unlinking an Account does not change the `MembershipId`.
5. One Account may participate in multiple Households through different
   Memberships.
6. Household authorization uses the current Household membership and its role.
7. Family relationships are separate from authorization roles and never grant
   authority implicitly.
8. `Owner`, `Member`, and `Guest` are the initial authority roles.
9. Household-scoped entities reference `MembershipId` when Household
   participation, assignment, or attribution is the relevant identity.
10. Authentication identity comes from trusted server-side authentication
    context. A caller-supplied `AccountId` or `UserId` is never proof of the
    caller's identity.

These are the proposed target decisions for the model. Partial source code does
not approve or fully enforce them; explicit review and follow-up work remain.

## Proposed Conceptual Model

```text
Account
  AccountId
      |
      | optional link
      v
HouseholdMember
  MembershipId
  HouseholdId
  AccountId?
  Role
```

Conceptually, one Account can link to zero or many Household Memberships, and
one Household contains one or many Household Memberships:

```text
Account 1   -> 0..N HouseholdMemberships
Household 1 -> 1..N HouseholdMemberships
```

This model describes domain identity and cardinality. It is not a final database
schema.

## Invariants

The following invariants are part of the target decision but still require
implementation and, where applicable, persistence and concurrency enforcement:

- `MembershipId` is never `Guid.Empty`.
- `MembershipId` remains stable for the lifecycle of the Membership.
- A non-null `AccountId` references a valid Account through an appropriate
  cross-module validation or integrity strategy.
- An Account cannot have duplicate active Memberships in the same Household.
- A Household preserves at least one active `Owner`.
- Linking or unlinking an Account preserves assignments and history attributed
  to the `MembershipId`.
- A family relationship does not grant authority implicitly.

The inspected repository baseline confirms only parts of the surrounding model,
such as the `Owner` / `Member` / `Guest` vocabulary and an in-memory duplicate
check based on the former `UserId` identity. It does not establish the
Membership invariants above.

## Why Not UserId?

`HouseholdMember.UserId` is semantically wrong as the durable membership
identity because it names an authentication-oriented identity, not a person's
participation in one Household. It requires every member to have an Account,
cannot preserve one membership identity across later account linking or
unlinking, and cannot by itself distinguish one Account's participation in
different Households. Historical attribution tied to it would also inherit the
Account lifecycle instead of the Membership lifecycle.

## Why Not Global Person Yet?

This ADR does not introduce a global `Person` entity or context. HomePlatform
does not yet have a proven cross-household profile lifecycle that requires one.
A Household-owned Membership with an optional Account link solves the current
identity problem with a smaller boundary. Person/Profile matching is deferred
until a concrete cross-household use case establishes its ownership, privacy,
matching, and lifecycle rules.

## Alternatives Considered

### A. Keep UserId as membership identity — Rejected

This preserves the current small model but conflates authentication and
Household participation. It cannot cleanly represent loginless members, later
linking, or stable Household history.

### B. Require every HouseholdMember to have an Account — Rejected

This would force credentials or invented Accounts onto children and other
participants who do not sign in. It makes the Account lifecycle a prerequisite
for Household modelling and still does not provide an independent durable
Membership identity.

### C. Introduce global Person now — Deferred / rejected for now

A global Person could separate a human profile from an Account, but it adds
cross-household ownership, matching, privacy, and lifecycle questions without a
validated need. It is broader than the problem that must be solved before the
membership schema.

### D. Stable MembershipId plus optional AccountId — Proposed decision

This directly supports loginless members, later verified linking, participation
in several Households, and stable attribution. It introduces one explicit
identity and a linking lifecycle, but keeps those concerns within the smallest
necessary model.

## DDD Rationale

In the target architecture, `Account` belongs to the Identity & Access language:
credentials, authentication, lockout, and Account lifecycle. `HouseholdMember`
or Membership belongs to the Households language: participation in one
Household, Household authority, and Household-scoped attribution.

Their lifecycles differ, so `MembershipId` is the durable identity of the
Household entity while `AccountId` is an optional reference to identity owned
outside the Households domain. This reduces accidental coupling between
authentication infrastructure and Household rules. These module or bounded
context responsibilities are architectural targets; this ADR does not claim
that fully separated bounded contexts already exist in code.

## Security Implications

- Resource authorization must resolve and verify the caller's current
  Membership in the target Household.
- Authentication of an Account alone is insufficient to authorize access to a
  Household resource.
- Knowing or guessing a `HouseholdId` does not grant access.
- A caller-supplied `AccountId` or `UserId` is never trusted as actor identity;
  the server obtains it from authenticated context.
- Linking and unlinking an Account requires an explicit, authorized workflow
  that verifies the Account and target Membership.
- Transitions that could remove or demote the last `Owner` require
  concurrency-safe enforcement in a later implementation.

## Persistence Implications

Future persistence work must give `HouseholdMember` a primary identity based on
`MembershipId` and permit a nullable `AccountId`. Active, Account-linked
Memberships will likely require uniqueness for `(HouseholdId, AccountId)` when
`AccountId` is present, without imposing global uniqueness on `AccountId`.

The foreign-key or cross-module reference strategy must preserve the rule that
Domain does not depend on Infrastructure. Assignments and history should store
`MembershipId` where Membership is the relevant identity. Exact tables, keys,
indexes, filtering for inactive Memberships, and cross-module integrity are not
decided here. EF Core mappings and migrations follow the Domain refactor; this
ADR creates none.

## Application Implications

The following use cases will need to adopt the decision without being
implemented by this ADR:

- `CreateHouseholdCommand` must carry business input, not a caller-selected
  creator identity.
- `CreateHouseholdHandler` must obtain the trusted Account actor from an
  authentication abstraction and create a distinct Owner Membership.
- `AddMember` must support a loginless Membership and, where applicable, an
  explicitly verified Account link.
- `LeaveHousehold` and `RemoveMember` must operate on `MembershipId`, authorize
  the actor through their own current Membership, and protect the last Owner.
- `LinkAccountToMembership` must be an explicit authorized use case that
  preserves `MembershipId`, assignments, and history and prevents duplicate
  active links within a Household.

## API Implications

Creator identity must eventually come from authenticated server-side actor
context, never from a request field. API contracts must not expose internal
Domain entities directly. Requests and results may use `AccountId` or
`MembershipId` only according to the use-case meaning: Account identity for an
explicit Account operation, and Membership identity for Household participation
or attribution.

## Testing Implications

Future tests must cover at least:

### Domain

- creating a loginless Membership;
- creating an Account-linked Membership;
- rejecting duplicate active Account Memberships in the same Household;
- preserving `MembershipId` after linking or unlinking an Account;
- preserving at least one Owner.

### Application

- a trusted actor creating the initial Owner Membership;
- the authorized Account-linking workflow, including duplicate and unauthorized
  attempts.

### Integration

- database constraints for identifiers, nullability, and scoped uniqueness;
- concurrency on Owner and Membership transitions;
- persistence round-trips for loginless, linked, and later-linked Memberships.

## Consequences

Positive consequences:

- Loginless Household members are representable without invented Accounts.
- A member can gain an Account later without losing identity or history.
- One Account can participate in multiple Households.
- Assignments and historical attribution retain a stable identity.
- The DDD boundary between authentication and Household participation is
  clearer.
- Authorization uses explicit Household authority rather than family labels.

Costs and negative consequences:

- The system contains more identifiers and concepts.
- Application and API boundaries require more explicit mapping.
- The Account-linking and unlinking lifecycle must be designed and implemented.
- Authorization must resolve Account to Membership for the target Household.
- Persistence mappings, constraints, and migrations become slightly more
  complex.

## Follow-up Work

1. Refactor `User` terminology to `Account` where it denotes authentication
   identity.
2. Refactor `HouseholdMember` to use `MembershipId` and optional `AccountId`.
3. Confirm `HouseholdRole` remains `Owner` / `Member` / `Guest`.
4. Implement Household membership and last-Owner invariants.
5. Update the `CreateHousehold` Application use case to use trusted actor
   context and create an Owner Membership.
6. Update Domain and Application tests.
7. Restore and verify a green build and test suite.
8. Only then create EF Core mappings and a migration.

## Non-Decisions

This ADR does not decide:

- a global Person/Profile context;
- a child guardian model;
- a granular ACL or capability engine;
- a Household-to-Household social graph;
- authentication token or cookie implementation;
- the final database schema;
- calendar or task assignment implementation.
