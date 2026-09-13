# HomePlatform Processing Register

Status: **Initial working register; legal and operational fields OPEN**

Reviewed: **2026-09-13**

## Scope

The [data inventory](DATA-INVENTORY.md) supplies repository evidence for the
activities below. This is not a completed legal register or a claim about
production processing. [DELETION-DESIGN.md](DELETION-DESIGN.md#purpose) defines
status terms. The prior audit's document recommendation is provided in the
task brief; unspecified audit conclusions are not inferred.

## Processing activities

| Activity | People / data | Purpose evidenced or planned | Implementation status | Article 6 basis |
|---|---|---|---|---|
| Account registration | Account holders; email/username, password passed to Identity, identity/security fields | Create credential-bearing Account | Implemented in source; production processing NOT VERIFIED | OPEN |
| Sign-in / refresh / lockout | Account holders; submitted credentials, protected tokens, security stamp and lockout state | Authenticate, renew access, limit failed sign-in | Implemented in source; current-Account validation on protected requests NOT YET IMPLEMENTED | OPEN |
| Household participation | Account-linked and loginless people; Household name/IDs/timestamps, MembershipId, AccountId, Role | Shared coordination and Household authority | Testing-only creation/model; resource authorization and leave/transfer/close NOT YET IMPLEMENTED | OPEN |
| Operational diagnostics | People identifiable through whatever runtime logs contain | Operate/debug/protect the service | Framework logging exists; content, redaction and production destination NOT VERIFIED | OPEN |
| Account deletion / privacy requests | Departing Account holder and affected people, including people without Accounts | ADOPTED lifecycle distinction; rights handling requires its own scope | DeleteAccount and wider rights workflows NOT YET IMPLEMENTED | OPEN |

## Unresolved fields for every activity

- **OPEN:** controller/legal entity and contact/address; responsible operational
  roles; whether a DPO is required and any resulting contact.
- **OPEN:** exact purpose-to-Article-6 assessment for each activity; do not infer
  that every activity relies on contract, consent or legitimate interests.
- **OPEN:** actual recipients/processors, contracts, provider/subprocessor
  choices, hosting/regions and any transfers/safeguards.
- **OPEN:** retention/erasure periods and backup/restore procedures. See
  [RETENTION-POLICY.md](RETENTION-POLICY.md).
- **OPEN:** final child-account policy and verification/representation handling
  for requests concerning loginless people.

Existing security controls and required proof are recorded in the
[security roadmap](../architecture/roadmap/SECURITY-ROADMAP.md). Do not copy
planned controls into this register as implemented guarantees. The legal
reference for later completion is [GDPR Articles 6 and 30](https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng).

## Proposed completion gate

**PROPOSED:** assign an accountable reviewer, validate each activity against
actual deployment/provider evidence, and record approved legal/operational
decisions with dates before real-user release. The production privacy/security
gate itself is **ADOPTED**; this document does not select the legal entity,
reviewer, processor, hosting region or lawful basis.
