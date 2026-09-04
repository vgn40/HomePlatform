# Migrations

EF Core migrations for the accepted first Household slice live here.
`InitialHousehold` creates Household and HouseholdMember with stable
MembershipId, nullable AccountId, and scoped uniqueness;
`LimitHouseholdNameLength` aligns the database with the 100-character Domain
bound; `AddIdentityPersistence` adds ASP.NET Core Identity's user, claim, login,
and token tables with Guid user keys. The repository pins dotnet-ef 10.0.11.

On 2026-09-04, the Testing integration host applied the migrations to fresh
PostgreSQL Testcontainers databases and EF reported no pending model changes.
See the [current next steps](../../../../../docs/architecture/roadmap/NEXT-STEPS.md)
before extending the schema.
