# HomePlatform.Domain

Domain is the framework-independent business core. It currently contains early
Household, HouseholdMember, HouseholdRole, User, and Result concepts.

Accepted
[ADR 0006](../../../docs/architecture/adr/0006-separate-account-and-household-membership-identity.md)
is implemented for the first slice: stable `MembershipId`, optional `AccountId`,
loginless Membership, Owner/Member/Guest roles, and scoped duplicate Account
links are represented and persisted. Link/unlink lifecycle, last-Owner
transitions, resource authorization, and non-downcastable collection exposure
remain incomplete. The
[domain model](../../../docs/architecture/target/DOMAIN-MODEL.md) separates
implemented, partial, target, and deferred concepts.
