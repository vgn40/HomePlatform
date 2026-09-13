# HomePlatform Data Inventory

Status: **Initial repository inventory; production data NOT VERIFIED**

Reviewed: **2026-09-13**, `main@07eeb12`

## Scope and evidence

This inventory describes code/schema capability, not observed production data.
Status terms and adopted boundaries come from [DELETION-DESIGN.md](DELETION-DESIGN.md#purpose).
The task brief supplied the prior audit's document recommendations; no separate
privacy-audit artifact was found in this repository.

Primary evidence:

- [Current model snapshot](../../backend/src/HomePlatform.Infrastructure/Persistence/Migrations/HomePlatformDbContextModelSnapshot.cs)
  and [Identity migration](../../backend/src/HomePlatform.Infrastructure/Persistence/Migrations/20260904100319_AddIdentityPersistence.cs).
- [Registration adapter](../../backend/src/HomePlatform.Infrastructure/Identity/IdentityAccountRegistration.cs),
  [sign-in adapter](../../backend/src/HomePlatform.Infrastructure/Identity/IdentityAccountAuthentication.cs),
  and [refresh adapter](../../backend/src/HomePlatform.Infrastructure/Identity/IdentityAccountRefresh.cs).
- [Household](../../backend/src/HomePlatform.Domain/Household/Household.cs),
  [HouseholdMember](../../backend/src/HomePlatform.Domain/Household/HouseholdMember.cs),
  and [API composition](../../backend/src/HomePlatform.Api/Program.cs).

## Current categories

| Data category / location | Fields or processing evidenced | People / purpose | Limits and lifecycle status |
|---|---|---|---|
| Account identity — AspNetUsers | Id, email/username and normalized forms, PasswordHash, SecurityStamp, ConcurrencyStamp, confirmation flags, lockout fields and failed count | Account holder; registration/authentication and access protection | Registration uses email as username; Account-owned data deletion with DeleteAccount ADOPTED but NOT YET IMPLEMENTED; operational retention periods OPEN |
| Additional Identity schema fields | PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled | Account holder, if later populated | Schema availability only; no current phone/2FA product workflow established by inspected endpoints |
| Identity dependent tables | AspNetUserClaims: type/value/UserId; AspNetUserLogins: provider/key/display name/UserId; AspNetUserTokens: provider/name/value/UserId | Account holder, if populated by an Identity feature | Schema does not prove external login providers, a session inventory, or storage of issued bearer tokens in these tables |
| Household | Id, Name, CreatedAt, UpdatedAt | Shared coordination context; free-text Name may contain personal data | CreateHousehold is Testing-only; CloseHousehold NOT YET IMPLEMENTED |
| HouseholdMember | MembershipId, HouseholdId, nullable AccountId, Role | Participation/authority; may represent a loginless child or other person | No name, age, birth date, or relationship field on this entity; null AccountId is not anonymization; DeleteAccount deletes all Account-linked memberships after ownership resolution, without loginless conversion (ADOPTED, NOT YET IMPLEMENTED) |
| Transient auth input/output | Submitted email/password; opaque bearer access/refresh tokens and their protected principal | Account holder; authenticate and renew access | Not Household data; client storage and exact access lifetime OPEN; no custom session store evidenced |
| Operational logging/configuration | Framework logging configuration, exception/ProblemDetails handling, readiness | Diagnostics/security; exact emitted identifiers depend on runtime | Production log content, destinations and retention NOT VERIFIED; redaction controls remain a security gate |

The [security roadmap](../architecture/roadmap/SECURITY-ROADMAP.md) records
current security gaps. No production database, real user records, logs, backups,
provider account, or hosting region was inspected for this inventory.

## Adopted classification and future additions

**ADOPTED:** classify Account-owned data, Membership/person-specific data,
Household-scoped/shared data, and personal references inside shared records.
Account-owned data and all memberships linked to the deleted AccountId are
deleted through DeleteAccount after ownership resolution. Other people's data,
including loginless people's data, needs its own assessment; Owner authority
does not make it the departing Account's data. AccountId remains nullable for
separate loginless-member creation/management.

Shared records follow an explicit feature/data-type lifecycle, not a generic
“someone still uses it” heuristic. Remove unnecessary personal references from
surviving records; a null reference alone does not prove anonymization. Data
without a documented continuing purpose must not be kept “just in case”.

Tasks, Shopping, Events, Routines, assignments and historical attribution are
future product scope, **NOT YET IMPLEMENTED**. Before adding each category,
extend this inventory with its actual fields, purpose, people, access,
deletion/retention behavior and source evidence. Complete the canonical
[feature lifecycle checklist](DELETION-DESIGN.md#feature-lifecycle-checklist),
including personal references, leave/deletion/closure, attribution UI and
export/privacy impacts. Do not describe illustrative future records as currently
collected or treat their concrete lifecycles as adopted.

## Open operational facts

Controller identity/contact/address, any DPO requirement/contact, processors,
production hosting and regions, transfers, actual recipients, Article 6 basis
per activity, child-account policy, retention and backup periods are **OPEN**.
Azure references elsewhere are proposed architecture options, not processor or
deployment evidence. See [PROCESSING-REGISTER.md](PROCESSING-REGISTER.md),
[RETENTION-POLICY.md](RETENTION-POLICY.md), and
[PRIVACY-NOTICE-REQUIREMENTS.md](PRIVACY-NOTICE-REQUIREMENTS.md).
