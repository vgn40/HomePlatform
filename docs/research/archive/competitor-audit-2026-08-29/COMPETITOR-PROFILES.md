# Competitor deep dives

> **ARCHIVED SUPPORTING EVIDENCE — NON-AUTHORITATIVE.** Research cutoff:
> 2026-08-29. See the [archive index](README.md).

Research cutoff: **2026-08-29**. Fifteen products were selected to cover the general family OS, multi-calendar, AI intake, chores, ambient display, co-parenting, grocery/meal, expenses, maintenance and platform baseline. `FACT` is always tied to the cited source; `INFERENCE` and `RECOMMENDATION` are explicitly marked.

## 1. Cozi

**Position.** Direct family organizer and one of the closest scope matches.

**FACT — product and workflow.** A household shares calendar, reminders, to-dos/chores, shopping, recipes and meals. Max adds AI event/recipe/meal-plan assistance. Up to 12 people use one family account. Every member can view, add, edit and delete; there is no restricted access. The official model uses one shared password, and removing a person requires changing that password [S001, S002].

**Onboarding and edge cases.** Account creation creates the family account; people are added by email/name. The shared credential makes invite, remove, leave and ownership-transfer semantics weak. An email can only belong to one Cozi account, so multiple household membership is explicitly constrained [S002].

**Monetization.** Free; Gold USD 39/year; Max USD 79/year per family account [S001].

**Pain signal.** Current reports include events disappearing/not appearing to all members, old-calendar limits and manual re-entry. Treat them as self-selected reports, not prevalence [S004].

**Security/privacy.** The privacy notice describes child-registration restrictions and access/copy/delete rights. No public evidence established MFA. Marketing wording was insufficient to classify the service as end-to-end encrypted [S003]. Core architecture: `UNKNOWN`.

**INFERENCE / HomePlatform response.** Copy the household entitlement and broad daily loop; reject the shared password, one-account-only email and unrestricted member access.

## 2. FamilyWall

**Position.** Broad direct competitor with the fullest suite among general products.

**FACT — product and workflow.** Users create or join a family, invite members, add a child without email and create additional circles. The no-email child still receives a login/password. Calendar, tasks/to-dos, shopping, meals, timetable, budget, documents, location and messaging are available; Google/Outlook sync, meals, budget and location alerts are premium [S005–S007, S088].

**Onboarding and edge cases.** Multiple circles provide a concrete adjacent multi-context pattern, not proof that FamilyWall models each circle as a Household. Child-without-email is explicit but credentialed; Founder, Administrator and member roles are documented, while fine-grained authorization remains `PARTIAL` [S007, S088].

**Monetization.** Official web snapshot USD 4.99/month or 44.99/year. Channel and region variation means a normalized global price remains `UNKNOWN` [S006].

**Pain signal.** Store reports recur around save/reset failures, task state, widgets, notifications and location accuracy [S009].

**Security/privacy.** The privacy notice describes precise location inputs, GDPR rights and general safeguards; detailed MFA/E2EE evidence was not found. Family Map itself warns that location may be stale/imprecise and is not an emergency locator [S008]. Core architecture: `UNKNOWN`.

**INFERENCE / HomePlatform response.** Multiple circles validate flexible contexts. The no-email-but-credentialed child shows that email identity and loginless membership are separate decisions; the product's breadth also demonstrates why location, budgets and meals should not dilute the first habit loop.

## 3. Family Tools

**Position.** Direct organizer with the strongest documented non-account and permission model.

**FACT — product and workflow.** Parent, Standard and Linked accounts support adults and children with or without logins. A Linked account can later upgrade. Parent/Teen/Child/Young Child presets and premium per-user customization control actions. Chores, recurrence, assignments, approvals/history, calendar, lists, meals and a dashboard are present [S010–S013].

**Onboarding and edge cases.** A parent creates the family and linked profiles, then can turn a profile into an account without redefining the family member. This is the closest public analogue to “membership identity survives later account linking” [S011].

**Monetization.** Free or USD 2.50/month / 25/year per family [S010].

**Pain signal.** Reports include notification failures, calendar re-entry/integration, locale date ambiguity and freezing; some evidence is older, so the signal is `MEDIUM` [S014].

**Security/privacy.** Parent control and loginless children are documented; transit encryption/deletion request appear in store disclosures. MFA, E2EE and self-service full export are `UNKNOWN`. Core architecture: `UNKNOWN`.

**INFERENCE / HomePlatform response.** Validate a stable MembershipId plus optional AccountId. Do not jump directly to a global Person context or arbitrary ACL engine.

## 4. Jam Family Calendar

**Position.** Direct organizer that combines family roles with controlled AI ingestion.

**FACT — product and workflow.** Adult, Teen, Big Kid, Little Kid and Caregiver participation is documented; a caregiver can be limited to selected calendars and younger children can exist without their own login. Users coordinate calendar events, drivers, “bring” items, lists and recurring to-dos. Email, text, voice or image input becomes a proposed structured draft that the user reviews, edits and confirms [S015–S017].

**Onboarding and edge cases.** The invite workflow assigns a family role and calendar visibility. This is concrete evidence that caregiver access and family relationship labels affect onboarding, but it does not prove HomePlatform needs the same taxonomy [S016].

**Monetization.** USD 15.99/month or 120/year, up to 12 members. Public trial/card wording has varied; exact current trial requirements should be rechecked before a commercial decision [S075].

**Security/privacy.** Detailed public security architecture was not established. Core architecture: `UNKNOWN`.

**INFERENCE / HomePlatform response.** The valuable AI pattern is draft-with-confirmation, not autonomous mutation. Caregiver-scoped calendars are a later product question, not a Phase 1 ACL mandate.

## 5. TimeTree

**Position.** Adjacent but strategically important multi-calendar product.

**FACT — product and workflow.** An account can participate in many shared calendars; profile/name can vary by calendar. Shared events support comments/photos and calendar chat. The Creator can remove members; members can leave; a departing Creator triggers succession; the shared calendar is deleted after all members leave [S019, S020, S092, S093].

**Onboarding and edge cases.** Create a calendar, invite by supported channels, accept, then operate inside that calendar. The product makes context switching explicit. It does not prove a server-side “active household” domain state is needed.

**Monetization.** Free; Premium USD 4.49/month or 44.99/year per account, without a shared family subscription [S021].

**Pain signal.** The official 2025–26 bug register records missing external events, duplicate notifications, slow changes and visibility/widget problems: `HIGH` failure-mode recurrence, not population prevalence [S025].

**Security/privacy.** Official claims include governance, audits, access review, vulnerability checks, encrypted channels and encryption at rest. MFA was not established [S022].

**Architecture evidence.** `CONFIRMED` historical 2020 stack: native iOS/Android, React/Redux, Rails/Sidekiq, Aurora MySQL, Redis, DynamoDB and AWS. Official 2026 material confirms a DynamoDB-to-Spanner/search direction; do not assume every 2020 component remains current [S023, S024].

**INFERENCE / HomePlatform response.** Multiple contexts are table stakes for non-nuclear arrangements. Calendar sync is a semantic reliability problem before it is an infrastructure choice.

## 6. Any.do Family

**Position.** Adjacent task product with an explicit private/shared split.

**FACT — product and workflow.** Each member keeps a private Personal Space and joins a Family Space. Admin/member/guest and board-level roles exist. Tasks, recurrence, multiple assignees, calendar, grocery, boards, board/task-scoped chat and mentions cover the core coordination loop [S026, S027, S094].

**Onboarding and edge cases.** An owner upgrades, creates the shared space and invites members. The Family plan is capped at four members and four shared projects; child-specific identity is not documented [S026, S027].

**Monetization.** USD 9.99/month or USD 8.33/month billed annually for up to four members [S026].

**Pain signal.** Current reports describe cross-device loss of reminders/subtasks and mismatch between expected family-wide sharing and board-by-board/capped sharing [S029].

**Security/privacy.** Official guidance states transit encryption, a verification code on a new login session, GDPR copy and deletion. This is more specific than most direct competitors, but was not independently tested [S028]. Core architecture: `UNKNOWN`.

**INFERENCE / HomePlatform response.** A private/personal versus household-shared boundary may matter later. Avoid a four-member or global-one-household data-model limit even if commercial tiers use limits.

## 7. Ohai.ai

**Position.** AI-first household assistant and ingestion reference.

**FACT — product and workflow.** Calendar, tasks, reminders, email/PDF/photo ingestion, meals and grocery/Instacart workflows are marketed. Individual, Duo and Group plans provide shared access, but detailed child/guest/role semantics were not established [S030].

**Onboarding and edge cases.** Users connect or forward source material, receive structured assistance and coordinate through a shared plan. Which actions require confirmation and how conflicts are resolved are not sufficiently public; mark them `UNKNOWN` rather than assuming autonomous execution.

**Monetization.** Free trial; public snapshot cited Individual USD 9.99/month, Duo 19.99/month and Group 29.99/month. Recheck before commercial use [S076].

**Security/privacy and architecture.** The targeted public evidence did not establish MFA, detailed child controls or the core architecture: `UNKNOWN`.

**INFERENCE / HomePlatform response.** Ingestion can reduce re-entry, but reliability, provenance and confirmation should precede AI. Do not move AI into the first six-month core.

## 8. Sweepy

**Position.** Chore/cleaning specialist that exposes routine and workload semantics.

**FACT — product and workflow.** Users create a household/home, rooms and cleaning tasks; dirtiness/frequency drives a schedule. Family members can be assigned, children can require approval, and points/streaks/leaderboards support gamification. Premium enables household sync and generated daily scheduling [S032, S033].

**Onboarding and edge cases.** A user seeds rooms/tasks or uses defaults, invites the household, then works from a daily plan. Multiple homes and role granularity are only partially evidenced.

**Monetization.** US App Store snapshot USD 3.99/month or 19.99/year with a short trial; store/region variation applies [S087].

**Pain signal.** Three current locale review sets report offline loops, missing schedule and wrong user/account state: `HIGH` current failure-mode recurrence [S033].

**Security/privacy.** Store disclosure says transit encryption and deletion request; detailed MFA/E2EE/export evidence is `UNKNOWN`. Core architecture: `UNKNOWN`.

**INFERENCE / HomePlatform response.** Recurrence, assignment and completion history are core. Points, leaderboards and auto-balancing are specialist features to validate later.

## 9. Skylight Calendar

**Position.** Hardware/wall-display benchmark for ambient household coordination.

**FACT — product and workflow.** Profiles, synced calendars, chores/routines, shopping/lists, rewards, meals and a wall dashboard are supported. Sidekick ingests photos, documents, email and voice into proposed household information [S034, S035].

**Onboarding and edge cases.** Buy/activate device, create profiles, connect external calendars, map people/colors and optionally link several devices. Current reports repeatedly describe calendar-to-person mapping, timezone and sync problems [S034, S038].

**Monetization.** Hardware tiers observed around USD 169.99/299.99/599.99; Plus around USD 79/year. Discounts and separate assistant SKUs make this a volatile snapshot [S036].

**Security/privacy.** Privacy material claims GDPR rights and an encrypted protocol for cloud media; detailed MFA/E2EE evidence was not found [S037]. Core architecture: `UNKNOWN`.

**INFERENCE / HomePlatform response.** Ambient Today has value, but hardware is a distraction. Build a responsive tablet/kiosk surface only after the core shared loop is reliable.

## 10. OurFamilyWizard

**Position.** Co-parenting specialist with rigorous roles, schedule-change workflows and records.

**FACT — product and workflow.** Separate parent accounts, restricted child/third-party/practitioner access, parenting schedules, holidays, change requests, pickup/dropoff, messages, expenses and records are documented [S039–S041].

**Onboarding and edge cases.** Each parent subscribes separately; linked parent accounts coordinate through the child/family case. Child name is required while email/phone can be optional. Children cannot see parent-to-parent correspondence, calls, expenses or protected information [S039, S040].

**Monetization.** Per parent per year: USD 110 / 149.99 / 216 / 299.88 depending on plan; restricted child/third-party/professional accounts are free [S039].

**Pain signal.** Current complaint samples repeat login, ToneMeter expectation and billing/refund issues. These are self-selected, not population incidence [S065].

**Security/privacy.** Official claims cover TLS, database encryption/redaction, affirmative location capture and privacy rights. Erasure may be constrained by shared/legal records. A password, security question, four-digit support code or app PIN is not evidence of MFA; public MFA remains `UNKNOWN` [S041, S073]. Core architecture: `UNKNOWN`.

**INFERENCE / HomePlatform response.** Transfer/leave/revoke and pickup-change requests are valuable domain patterns. Court-grade immutability and per-parent pricing do not fit the ordinary-household core.

## 11. AppClose

**Position.** Co-parenting specialist and strongest non-user/circle workflow reference.

**FACT — product and workflow.** Users create circles, add children/co-parents/third parties, coordinate parenting schedules, events, requests, expenses, reimbursements, messages and calls. AppClose Solo can send events/requests to non-users while preserving the initiator's record [S042, S043].

**Onboarding and edge cases.** Circle membership and intentional third-party sharing are explicit. Records, exports and deletion are designed for a legal domain; shared communications can remain while another participant's account exists [S043, S044].

**Monetization.** USD 7.99/month on web or 8.99/month in app, 60-day trial; fee waivers are advertised [S042].

**Security/privacy.** Official claims include 2FA, PIN/biometric lock, TLS, encryption in transit/at rest, private location and access controls. These are vendor claims, not independent assessment [S042–S044]. Core architecture: `UNKNOWN`.

**INFERENCE / HomePlatform response.** Multiple circles and invitation-to-non-user validate flexible network edges. Do not import legal-record permanence, payments or court workflows into the six-month plan.

## 12. AnyList

**Position.** Specialist reference for a high-trust shared shopping and meal loop.

**FACT — product and workflow.** Create/share lists by account/email, edit in real time, import/manage recipes, plan meals and push ingredients into a shopping list. Meal-plan sharing is a household entitlement; child/guest permissions are not publicly detailed [S045–S047].

**Onboarding and edge cases.** The loop is unusually short: create list, invite, add/check items. Recipes and meal planning layer on later without changing the core list interaction.

**Monetization.** Core list sharing is free; AnyList Complete snapshot USD 9.99/year individual or 14.99/year household [S045].

**Security/privacy and architecture.** Detailed MFA/E2EE/core architecture evidence was not established: `UNKNOWN`.

**INFERENCE / HomePlatform response.** Shopping should be fast, optimistic and recoverable. Start with one list if needed, but avoid a schema/API assumption that a household can never have multiple lists.

## 13. Splitwise

**Position.** Specialist reference for shared financial facts, multi-groups and recurrence.

**FACT — product and workflow.** Users create groups, record expenses with several split methods, maintain balances, settle and schedule recurring expenses. Any participant who can see a shared expense may edit/delete/undelete it; account deletion leaves non-personal shared records with other participants [S048–S051].

**Onboarding and edge cases.** A user can belong to several groups. Groups have no admin/special permission model and the creator has no special authority. Attribution and record survival are distinct from account existence. This is relevant to household history even if expenses are deferred [S090].

**Monetization.** Free is limited to four new expenses/day; Pro removes limits and adds capabilities. Exact current price is region/channel dependent and therefore `UNKNOWN` in this audit [S050].

**Pain signal.** Repeated current reviews object to the daily limit and wait/paywall: `HIGH` in the sampled failure mode [S064].

**Security/privacy.** Official privacy/export material covers shared-record behavior, CSV/JSON export and deletion. MFA was not established [S051].

**Architecture evidence.** Android Kotlin is `CONFIRMED` by a first-party role; Rails is `STRONGLY INDICATED`, not confirmed as the whole backend. Core topology remains `UNKNOWN` [S068, S069].

**INFERENCE / HomePlatform response.** Keep membership exit distinct from historical attribution. Expenses remain P2; do not generalize their mutability rules across all contexts.

## 14. Dwellin

**Position.** Home-maintenance specialist and reference for asset/document lifecycles.

**FACT — product and workflow.** Onboarding collects home information, builds an annual care calendar and manages tasks, appliances, manuals, warranties, receipts, service history and documents. Family invitations and multiple properties are supported to some degree [S052, S053].

**Onboarding and edge cases.** The product starts from the asset/property and derives future maintenance, unlike a generic task list. Primary/Sub-Account Holder roles and an adult account-holder requirement are documented; child-profile, guest-capability and household-to-household sharing behavior remain `UNKNOWN` [S091].

**Monetization.** Free core; Prime Rewards snapshot USD 99/year [S053].

**Security/privacy and architecture.** Detailed public controls and core architecture remain `UNKNOWN`.

**INFERENCE / HomePlatform response.** Maintenance is a legitimate later bounded language (asset, warranty, service record), not extra fields on HouseholdTask. Defer it until the coordination core has adoption.

## 15. Google Family

**Position.** Platform baseline that shapes user expectations and exposes restrictive family-group semantics.

**FACT — product and workflow.** A family manager creates a group of at most six, invites members and can create/supervise child accounts. Only one family group at a time is allowed, with switching restrictions. A family calendar is automatically created; members can edit it and ownership/visibility have specific rules [S054, S055].

**Onboarding and edge cases.** Child-account and family-role flows are mature, but the one-family-group assumption conflicts with blended, extended and caregiving networks. Calendar sharing outside the family is possible, so “household” and “calendar audience” are not identical [S054, S055].

**Monetization.** No separate organizer charge was identified in the reviewed family-group/calendar sources; storage and other Google services were outside this pricing check.

**Security/privacy and architecture.** This audit did not attempt to restate Google's broad platform architecture. Product-level family controls are evidenced; implementation details relevant to HomePlatform are `UNKNOWN` in the scoped evidence.

**INFERENCE / HomePlatform response.** Match familiar invite/child/calendar expectations, but do not copy one-family-per-account or platform-wide manager semantics.

## Standardized workflow coverage

This table forces the same 16 requested workflows across every deep-dive product. `Y` means a source-backed product flow exists, `P` means an adjacent/limited/paywalled flow, `X` is an explicit constraint and `U` is not established. It is a coverage map; the product sections above and the cross-product workflow table in the main audit document the observed steps and edge cases.

| Product | Create household | Invite partner | Add child | Add grandparent | Create task | Recurring chore | Assign | Complete | Create event | Pickup | Add shopping item | Plan week | Today | Remove member | Leave | Transfer owner |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Cozi | Y | P | P | P | Y | P | Y | Y | Y | U | Y | Y | Y | P | U | U |
| FamilyWall | Y | Y | Y | P | Y | P | Y | Y | Y | P | Y | Y | Y | P | U | U |
| Family Tools | Y | Y | Y | P | Y | Y | Y | Y | Y | P | Y | Y | Y | Y | U | U |
| Jam | Y | Y | Y | P | Y | Y | Y | Y | Y | Y | Y | Y | Y | P | U | U |
| TimeTree | P | Y | U | P | U | P | U | U | Y | P | U | Y | Y | Y | Y | P |
| Any.do Family | Y | Y | U | P | Y | Y | Y | Y | Y | P | Y | Y | Y | Y | U | U |
| Ohai | P | P | U | U | Y | P | P | P | Y | P | P | Y | Y | U | U | U |
| Sweepy | Y | Y | P | P | Y | Y | Y | Y | U | U | U | Y | Y | P | P | U |
| Skylight | Y | Y | Y | P | Y | Y | Y | Y | Y | P | Y | Y | Y | P | U | U |
| OurFamilyWizard | Y | Y | Y | Y | P | P | P | P | Y | Y | U | Y | Y | Y | P | U |
| AppClose | Y | Y | Y | Y | P | P | P | P | Y | Y | U | Y | Y | P | P | U |
| AnyList | P | Y | U | P | U | U | U | P | U | U | Y | Y | P | P | P | U |
| Splitwise | P | Y | U | P | U | P | P | P | U | U | U | U | P | P | U | U |
| Dwellin | Y | Y | U | P | Y | Y | Y | Y | P | U | U | Y | Y | P | P | U |
| Google Family | Y | Y | Y | P | U | P | U | U | Y | P | P | Y | P | Y | Y | P |

### Workflow edge-case notes by product

- **Cozi:** removal is a shared-password reset rather than scoped revocation; leave/transfer are `UNKNOWN` [S002].
- **FamilyWall:** child-without-email and extra-circle creation are explicit; leave/transfer semantics were not established [S007].
- **Family Tools:** Parent removal and Linked profile → account upgrade are explicit; self-leave and transfer remain insufficiently public [S011].
- **Jam:** caregiver calendar scope and reviewed assistant drafts are explicit; owner transfer is `UNKNOWN` [S016, S017].
- **TimeTree:** Creator removal, member leave, Creator succession and deletion after all members leave are explicit calendar-context lifecycles; multiple calendars are still not proof of a Household-to-Household model [S019, S020, S092, S093].
- **Any.do:** private Personal Space remains separate from Family Space; fixed four-member/four-project limits constrain the flow [S026, S027].
- **Ohai:** intake/planning is public, but child, removal, leave, transfer and conflict behavior are `UNKNOWN` [S030, S076].
- **Sweepy:** cleaning completion/approval and smart scheduling are explicit; general event/shopping/ownership workflows are outside its evidenced scope [S032, S033].
- **Skylight:** profile/calendar mapping is central and a repeated pain; ownership transfer was not established [S034, S038].
- **OurFamilyWizard:** separate parent and restricted child/third-party accounts support two-home workflows; account deletion/record retention is intentionally constrained [S039–S041].
- **AppClose:** Circle/Solo supports third parties and non-users; legal-record lifecycle is specialist behavior, not a generic Household transfer model [S042–S044].
- **AnyList:** create/share/add/check is the strongest verified workflow; membership and ownership lifecycle are comparatively undocumented [S045–S047].
- **Splitwise:** groups, recurring expense and shared-record history are evidenced; self-leave is `UNKNOWN`, and the established flows are expense-specific rather than chores/events [S048–S051].
- **Dwellin:** onboarding creates a home care plan from property/assets; child and ownership-transfer semantics are `UNKNOWN` [S052, S053].
- **Google Family:** supervised child Account and one-family-group lifecycle are explicit; they are not loginless membership [S054, S055, S085].

## Cross-profile conclusion

**FACT.** All 15 have at least one source-backed collaboration workflow. Only TimeTree exposes a deep public technology snapshot; Todoist (outside the deep 15) exposes an instructive sync protocol, while most competitor core architectures remain `UNKNOWN`.

**INFERENCE.** The durable common denominator is not a giant family suite. It is: flexible membership, a short invite-to-shared-value loop, calendar/task recurrence, trustworthy list state, assignment/reminders and clear context/permission semantics.

**RECOMMENDATION.** HomePlatform should correct membership identity before the first durable schema, preserve the modular monolith, keep the core roadmap small, and add product/design decision gates for multi-household, recurrence, notification semantics and calendar integration. It should not build AI, hardware, location, legal records, expenses, meals or a generic ACL platform in the first six months.
