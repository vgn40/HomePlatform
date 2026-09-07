# HomePlatform.Domain

Domain is the framework-independent business core. It currently contains early
Household and HouseholdMember entities, the HouseholdRole enum, and a small
Result helper. The former Domain User has been removed; Account credentials
belong to ASP.NET Core Identity in Infrastructure. No Account aggregate, explicit
value objects, Domain Services, or Domain Events are currently justified.

Accepted
[ADR 0006](../../../docs/architecture/adr/0006-separate-account-and-household-membership-identity.md)
is implemented for the first slice: stable `MembershipId`, optional `AccountId`,
loginless Membership, Owner/Member/Guest roles, and scoped duplicate Account
links are represented and persisted. Link/unlink lifecycle, last-Owner
transitions, and resource authorization remain incomplete. Members are exposed
through a non-downcastable live read-only view. The
[domain model](../../../docs/architecture/target/DOMAIN-MODEL.md) separates
implemented, partial, target, and deferred concepts.
