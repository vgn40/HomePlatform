# HomePlatform Retention Policy

Status: **Initial design; retention periods OPEN; enforcement NOT YET IMPLEMENTED**

Reviewed: **2026-09-13**

## Adopted boundaries

[DELETION-DESIGN.md](DELETION-DESIGN.md) owns the **ADOPTED** lifecycle rules:
DeleteAccount resolves ownership, explicitly deletes all Account-linked
memberships across all Households without loginless conversion, deletes
Identity/ApplicationUser and Account-owned data, and denies new protected
product requests after commit. Other people's data and legitimate shared
records follow their separate feature lifecycle.
No grace period is adopted; do not build one without a later product decision.
Immediate access denial is not proof that every backup or future shared record
has already been physically erased.

The [data inventory](DATA-INVENTORY.md) is source/schema evidence only. No
production retention configuration or executed deletion schedule has been
verified. This document does not approve indefinite retention.

## Initial retention decisions

| Category | Adopted constraint | Unresolved policy |
|---|---|---|
| Account identity/credentials | Delete Account-owned email, username, password hash and security/account state with Identity after ownership resolution and linked-membership deletion | Operational retention outside the live Account store, including backup periods, OPEN |
| Account-linked HouseholdMember memberships | Delete all through DeleteAccount after ownership resolution; no automatic loginless conversion; AccountId stays nullable for separate loginless-member flows | DeleteAccount membership fate RESOLVED; implementation missing |
| Other person-specific records | Delete under the feature rule if no continuing product purpose without the person; no retention “just in case” | Concrete feature lifecycle and retention periods OPEN |
| Other people's data / continuing shared Household | One Account's deletion cannot automatically remove a legitimately continuing shared Household | Category-specific retention and privacy handling OPEN |
| Explicit CloseHousehold | Explain Household-data deletion; assess loginless people and shared data | Immediate hard deletion of every future record type OPEN |
| Future attribution/history | Remove unnecessary personal references from surviving shared records under explicit feature rules; do not retain names/emails just for attribution or call nulling anonymization | Concrete fields/history behavior, feature-specific hard deletion and periods OPEN |
| Logs/security diagnostics | Follow security roadmap's minimization/redaction gates | Actual content, destinations, access and retention periods OPEN |
| Backups/restore copies | Must be addressed before real-user release | Provider capability, expiry, deletion propagation and restore handling OPEN |

Every persisted feature must complete the
[feature lifecycle checklist](DELETION-DESIGN.md#feature-lifecycle-checklist).
Decide continued purpose and record fate per feature/data type, not through a
generic runtime usage heuristic. Account/creator deletion is not a blanket
cascade rule for legitimate shared data. Missing-attribution text such as
“Tidligere medlem” is UI presentation, not a replacement identity to retain.

## Proposed implementation preparation

**PROPOSED**, pending later policy decisions:

- Maintain a per-category retention schedule with purpose, trigger, duration,
  deletion method, exceptions and verification responsibility.
- Define and test how a restore avoids unintentionally reactivating deleted
  Accounts or retained access; choose the smallest necessary mechanism.
- Verify retention/deletion behavior with the selected providers before making
  user-facing promises about backup erasure.

No numerical retention duration, backup period, grace period, hosting choice,
or legal basis is settled here. Resolve these **OPEN** fields through the
[processing register](PROCESSING-REGISTER.md) before the affected release gate,
and align the eventual [privacy notice](PRIVACY-NOTICE-REQUIREMENTS.md).
