# ADR 0006: Separate Account and Household Membership Identity

Status: Accepted
Accepted: 2026-08-30

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

This decision was required before the first durable membership mapping and
migration. The modular monolith and its Domain/Application/Infrastructure/API
layering remain unchanged.

### Implementation status

The decision has now been adopted by the implemented Household slice.

Implemented and verified:

- `HouseholdMember` has a stable `MembershipId`.
- `AccountId` is optional.
- Household creation creates a distinct Owner Membership.
- Creator identity comes from trusted server-side actor context rather than
  request data.
- Duplicate non-null Account links are protected in the Household aggregate.
- PostgreSQL enforces scoped uniqueness for `(HouseholdId, AccountId)`.
- EF Core mappings and the `InitialHousehold` migration persist the model.
- PostgreSQL integration tests prove save/reload, `MembershipId` preservation,
  loginless Memberships, and stale-context duplicate protection.
- The Testing-only HTTP slice proves that the authenticated Account becomes
  the initial Owner and caller-supplied `AccountId` does not choose actor
  identity.

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

These are the accepted identity and authority decisions for the Household
model. Acceptance records the chosen model; it does not claim that every
invariant or follow-up use case is implemented.

## Current-decision addendum — 2026-09-13

**ADOPTED.** The original context and implementation evidence above are retained.
[DELETION-DESIGN.md](../../privacy/DELETION-DESIGN.md) is canonical for subsequent
deletion/lifecycle decisions and narrows the earlier open integrity direction.

- AccountId is optional because MembershipId and Account identity have separate
  lifecycles. Optional AccountId is not permission for a loginless Owner;
  Owner requires a real Account, including after deletion or unlinking.
- Membership never implies ownership. Children, grandparents, partners,
  Members and Guests never become Owner automatically.
- Ownership transfer is explicit and chooses a concrete eligible Account-linked
  destination. Destination acceptance remains OPEN.
- DeleteAccount resolves every Household membership. A last Owner must choose
  explicit TransferOwnership or CloseHousehold; a continuing Household cannot
  be ownerless. A non-last-Owner departure does not automatically change any
  other person's role.
- Add a nullable HouseholdMember.AccountId FK to AspNetUsers.Id with rejecting
  deletion behavior. Account-to-HouseholdMember CASCADE DELETE and automatic
  SET NULL are not lifecycle logic. The exact EF/PostgreSQL mapping remains an
  OPEN implementation detail; Domain must stay framework-independent.
- **Follow-up ADOPTED decision — 2026-09-13:** DeleteAccount membership fate is
  resolved. After ownership rules are resolved, explicitly delete every
  HouseholdMember linked to the AccountId across all Households, then delete
  Identity/ApplicationUser. Never silently convert those memberships to loginless.
  AccountId stays nullable for separately created/managed loginless memberships.
- Stable MembershipId preserves identity while the Membership exists; it does
  not require preserving the Membership after DeleteAccount. The earlier
  link/unlink/history language is not an indefinite retention rule. Concrete
  future feature attribution/history remains OPEN; surviving shared records
  must lose unnecessary personal references under their explicit feature rules.
- DeleteAccount, LeaveHousehold, TransferOwnership, CloseHousehold, the Account
  FK and protected-request current-Account validation are NOT YET IMPLEMENTED.

The original “preserve at least one Owner” invariant applies to a continuing
Household. Explicit CloseHousehold ends that lifecycle; it does not authorize
an ownerless Household to continue. CloseHousehold's detailed handling of every
future record type remains OPEN. Every new persisted feature must complete the
[lifecycle checklist](../../privacy/DELETION-DESIGN.md#feature-lifecycle-checklist);
creator deletion alone does not determine shared record ownership or retention.

## Conceptual Model

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

## Accepted invariants and remaining implementation work

The following invariants are part of the accepted decision:

- `MembershipId` is never `Guid.Empty`.
- `MembershipId` remains stable for the lifecycle of the Membership.
- A non-null `AccountId` references a valid Account through an appropriate
  cross-module validation or integrity strategy.
- An Account cannot have duplicate active Memberships in the same Household.
- A Household preserves at least one active `Owner`.
- Linking or unlinking an Account preserves assignments and history attributed
  to the `MembershipId`.
- A family relationship does not grant authority implicitly.

The current Household slice establishes non-empty generated `MembershipId`
values, optional `AccountId`, distinct Owner Membership creation, and duplicate
non-null Account protection in both the aggregate and PostgreSQL persistence.
Verified linking and unlinking, last-Owner protection, concurrency-safe Owner
transitions, Account-reference validation, and assignment/history preservation
remain implementation work.

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

### D. Stable MembershipId plus optional AccountId — Accepted

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

The implemented persistence model gives `HouseholdMember` a primary identity
based on `MembershipId` and permits a nullable `AccountId`. PostgreSQL enforces
uniqueness for `(HouseholdId, AccountId)` without imposing global uniqueness on
`AccountId`; PostgreSQL's null semantics allow multiple loginless Memberships
in one Household.

The foreign-key or cross-module reference strategy must preserve the rule that
Domain does not depend on Infrastructure. Assignments and history should store
`MembershipId` where Membership is the relevant identity. Exact tables, keys,
indexes, filtering for inactive Memberships, and cross-module integrity beyond
the implemented initial Household schema are not decided here.

## Application Implications

The implemented `CreateHousehold` use case carries business input only, obtains
the Account actor from trusted server-side context, and creates a distinct Owner
Membership. `AddMember` supports loginless Memberships and optional Account
links, but it is not yet an authorized linking workflow.

The following use cases still need to adopt the decision:

- `LeaveHousehold` and `RemoveMember` must operate on `MembershipId`, authorize
  the actor through their own current Membership, and protect the last Owner.
- `LinkAccountToMembership` must be an explicit authorized use case that
  preserves `MembershipId`, assignments, and history and prevents duplicate
  active links within a Household.

## API Implications

The Testing-only Create Household HTTP slice obtains creator identity from the
authenticated server-side actor context, never from a request field, and does
not expose internal Domain entities. The production authentication mechanism
and production Create Household route are not implemented by this slice.
Future requests and results may use `AccountId` or `MembershipId` only according
to the use-case meaning: Account identity for an explicit Account operation,
and Membership identity for Household participation or attribution.

## Testing Implications

Implemented Domain, Application, and PostgreSQL integration tests cover:

- creation of loginless and Account-linked Memberships;
- non-empty `MembershipId` and persistence round-trips that preserve it;
- aggregate rejection and database-constraint rejection of duplicate non-null
  Account Memberships in the same Household;
- trusted actor creation of the initial Owner Membership; and
- rejection of anonymous or invalid Testing-host actor identity without writes.

Future tests must cover at least:

### Domain

- preserving `MembershipId` after linking or unlinking an Account;
- preserving at least one Owner.

### Application

- the authorized Account-linking workflow, including duplicate and unauthorized
  attempts.

### Integration

- concurrency on Owner and Membership transitions;
- persistence round-trips for later-linked and unlinked Memberships.

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

## Remaining follow-up work

1. Implement verified Account linking and unlinking while preserving
   `MembershipId`.
2. Protect the last Owner invariant, including concurrent transitions.
3. Validate Account references once the Identity boundary exists.
4. Implement Household resource authorization through Membership.
5. Preserve Membership attribution for future assignments and history.

## Non-Decisions

This ADR does not decide:

- a global Person/Profile context;
- a child guardian model;
- a granular ACL or capability engine;
- a Household-to-Household social graph;
- authentication token or cookie implementation;
- the future Household database schema beyond the implemented identity
  constraints;
- calendar or task assignment implementation.
