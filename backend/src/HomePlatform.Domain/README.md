# HomePlatform.Domain

Domain is the framework-independent business core. It currently contains early
Household, HouseholdMember, HouseholdRole, User, and Result concepts.

The current working tree now prototypes stable `MembershipId` plus optional
`AccountId`, but the handler/tests are not aligned and the governing ADR is
still Proposed. Do not describe or persist that prototype as an approved model
until
[ADR 0006](../../../docs/architecture/adr/0006-separate-account-and-household-membership-identity.md)
is reviewed. The [domain model](../../../docs/architecture/target/DOMAIN-MODEL.md)
separates implemented, proposed, and deferred concepts.
