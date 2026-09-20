# Migrations

EF Core migrations for the accepted first Household slice live here.
`InitialHousehold` creates Household and HouseholdMember with stable
MembershipId, nullable AccountId, and scoped uniqueness;
`LimitHouseholdNameLength` aligns the database with the 100-character Domain
bound; `AddIdentityPersistence` adds ASP.NET Core Identity's user, claim, login,
and token tables with Guid user keys. The fourth migration,
`20260913183105_AddHouseholdMemberAccountReference`, adds the nullable AccountId
FK to AspNetUsers.Id and the AccountId lookup index. ClientNoAction / PostgreSQL
NO ACTION guards unresolved Account deletion; it does not cascade or null links.
All four are committed at `main@7162c35`. The repository pins dotnet-ef 10.0.11.

Historical evidence only: on 2026-09-04, the Testing integration host applied the migrations to fresh
PostgreSQL Testcontainers databases and EF reported no pending model changes.
The 2026-09-14 FK review added valid/dangling upgrade evidence. Neither dated
run is a fresh pending-model or deployed-migration check for 2026-09-19.
See the [current next steps](../../../../../docs/architecture/roadmap/NEXT-STEPS.md)
before extending the schema.
