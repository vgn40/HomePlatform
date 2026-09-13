# HomePlatform Privacy Notice Requirements

Status: **Initial requirements; public notice NOT YET IMPLEMENTED**

Reviewed: **2026-09-13**

## Purpose and authority

This is a preparation checklist, not a publishable privacy notice. Use the
[data inventory](DATA-INVENTORY.md), [processing register](PROCESSING-REGISTER.md),
[retention policy](RETENTION-POLICY.md) and canonical
[deletion design](DELETION-DESIGN.md). Do not invent the missing legal entity,
address, DPO, processors, deployment or retention facts.

## Adopted product information

The eventual notice and deletion UX must accurately explain:

- **ADOPTED:** DeleteAccount, LeaveHousehold, TransferOwnership and
  CloseHousehold have different scopes; DeleteAccount explicitly deletes all
  Account-linked memberships after ownership resolution, without converting
  them to loginless memberships. Account-owned data is deleted with the Account.
- **ADOPTED:** last Owner requires explicit transfer or closure; no automatic
  promotion; loginless people, including children, may still have personal data.
- **ADOPTED:** Owner is a Household authority role, not ownership of other
  people's personal data; shared data is assessed separately from Account data.
- **ADOPTED:** explicit closure includes clear information that Household data
  is deleted; no grace period is adopted.
- **ADOPTED:** successfully committed DeleteAccount ends access for new
  protected product requests; this behavior is **NOT YET IMPLEMENTED**.
- **ADOPTED:** an Article 17 request can differ from product Account deletion
  and concern someone without an Account; do not promise that one button
  completes every privacy/legal request.

Describe surviving shared records under their explicit feature lifecycle and
the removal of unnecessary personal references. Do not promise that nulling
references anonymizes the remaining data or satisfies every erasure request.
Future Task/Shopping/Event/Routine examples are illustrative, not settled
feature behavior. Each feature must review notice and inventory impacts using
the [lifecycle checklist](DELETION-DESIGN.md#feature-lifecycle-checklist).

## Information to complete before publication

Review the applicable disclosure requirements in
[GDPR Articles 13 and 14](https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng):
controller/contact details, DPO where applicable, purposes and legal bases,
data categories/sources, recipients/transfers, retention criteria, rights and
complaints, and relevant information about required data or automated decisions.
Applicability and HomePlatform-specific content require completion.

| Missing HomePlatform fact | Status |
|---|---|
| Legal entity/controller, address and contact | OPEN |
| DPO applicability/contact | OPEN; do not assert appointment or exemption |
| Article 6 basis per activity | OPEN |
| Processors, hosting, region, recipients and transfers | OPEN; architecture proposals are not provider agreements |
| Retention and backup periods | OPEN |
| Concrete feature lifecycles and shared attribution/history | OPEN; DeleteAccount membership deletion and unnecessary personal-reference removal are ADOPTED |
| DeleteAccount reauthentication UX and request-verification process | OPEN |
| Child-account policy and information for loginless people | OPEN |
| Exact rights request entry points and operational handling | OPEN |
| Immediate hard-deletion coverage for future CloseHousehold record types | OPEN |

## Proposed publication workflow

**PROPOSED:** draft the notice after resolving the relevant processing-register
fields, check every factual promise against implemented flows and provider
evidence, and review how information reaches people whose data is supplied by
another Household participant. Record the approved version and review date.

The **ADOPTED** release gate requires production privacy/security readiness
before real users. A checklist, code inspection or local test pass does not
establish that readiness.
