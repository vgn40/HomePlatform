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
links are represented and persisted. TransferOwnership changes two roles while
preserving membership identities; Leave removes the caller Member/Guest and
refuses every Owner; Close checks Owner authority before Application requests
physical deletion. Typed Domain results expose these outcomes.

At committed `main@7162c35`, link/unlink lifecycle, concurrent ownership
protection and broader authorization remain incomplete. The affected Household
authorization tests passed locally on 2026-09-20;
[NEXT-STEPS](../../../docs/architecture/roadmap/NEXT-STEPS.md#verified-locally)
records the command and scope. Members are exposed
through a non-downcastable live read-only view. The
[domain model](../../../docs/architecture/target/DOMAIN-MODEL.md) separates
implemented, partial, target, and deferred concepts.
