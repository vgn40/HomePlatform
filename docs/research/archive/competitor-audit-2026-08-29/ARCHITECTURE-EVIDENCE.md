# Architecture evidence and comparison

> **ARCHIVED SUPPORTING EVIDENCE — NON-AUTHORITATIVE.** Point-in-time research
> and repository evidence from 2026-08-29. See the current
> [target architecture](../../../architecture/target/TARGET-ARCHITECTURE.md).

Research cutoff: **2026-08-29**.

## Evidence discipline

`CONFIRMED` requires first-party technical documentation, a verified repository, API specification, talk or job description that directly supports the statement. `STRONGLY INDICATED` is narrower evidence that does not prove the whole core. `WEAK SIGNAL` cannot support a design decision. `UNKNOWN` is the correct result when no credible public evidence was found.

Competitor architecture is descriptive only. It is not a reason to change HomePlatform's stack.

## Competitor architecture evidence

| Product | Confidence | Publicly supported signal | What remains unknown | Sources |
|---|---|---|---|---|
| TimeTree | `CONFIRMED` historical stack and 2026 migration direction | 2020: Swift/Objective-C, Kotlin/Java, React/Redux, Rails/Sidekiq, Aurora MySQL, Redis, DynamoDB and AWS. 2026: DynamoDB-to-Google Cloud Spanner and full-text-search work | current full topology; which 2020 components remain; boundaries and consistency model | S023, S024 |
| Todoist | `CONFIRMED` sync protocol; core stack `UNKNOWN` | local client state, incremental sync tokens, batched reads/writes, optimistic updates, temporary-ID mapping and idempotency request IDs | private backend language, database, queues and deployment topology | S062 |
| Splitwise | Android `CONFIRMED`; backend `STRONGLY INDICATED` | first-party roles support Kotlin Android; a server role values Rails and mobile API work | exact production backend/database/topology | S068, S069 |
| Bring! | hosting/integrations `CONFIRMED`; core `UNKNOWN` | privacy/careers evidence names AWS hosting, Firebase Analytics, AppsFlyer, Braze and an iOS engineering function | list sync protocol, database, backend language and deployment topology | S070, S071 |
| Tody | `WEAK SIGNAL`; core `UNKNOWN` | privacy material names Firebase/Google services | whether Firebase is core persistence or only analytics/support | S078 |
| OurFamilyWizard | integrations/security properties `CONFIRMED`; core `UNKNOWN` | public material names Twilio/Plaid and encryption/redaction behavior | languages, framework, database product and topology | S041 |
| Family Tools | `UNKNOWN` | reviewed product/account/permission sources do not establish core architecture | every concrete technology and topology | S010–S013 |
| Cozi | `UNKNOWN` | no sufficient first-party technical evidence found | all core technologies and boundaries | S001–S003 |
| FamilyWall | `UNKNOWN` | no sufficient first-party technical evidence found | all core technologies and boundaries | S005–S008 |
| Jam | `UNKNOWN` | product/help evidence only | all core technologies and boundaries | S015–S017 |
| Any.do | `UNKNOWN` | security behavior is public; core stack is not | all core technologies and boundaries | S026–S028 |
| Ohai | `UNKNOWN` | product/AI claims only | model/provider, storage, orchestration and core topology | S030 |
| Sweepy | `UNKNOWN` | store/product evidence only | all core technologies and boundaries | S032–S033 |
| Skylight | `UNKNOWN` | hardware/product/privacy evidence only | sync, device/cloud and data architecture | S034–S037 |
| AppClose | `UNKNOWN` | security/product claims only | all core technologies and boundaries | S042–S044 |
| AnyList | `UNKNOWN` | product/help evidence only | sync, offline and core topology | S045–S047 |
| Dwellin | `UNKNOWN` | product/help evidence only | all core technologies and boundaries | S052–S053 |
| Google Family | `UNKNOWN` | product-level family controls only | technologies, service boundaries, storage and consistency model relevant to the scoped workflows | S054, S055, S085 |

## Technology-signal inventory

| Signal area | Confirmed evidence | Decision relevance to HomePlatform |
|---|---|---|
| Native/mobile clients | TimeTree native iOS/Android historical; Splitwise Kotlin Android | descriptive only; HomePlatform has no implemented client |
| Web frontend | TimeTree React/Redux historical | no reason to alter backend boundaries |
| Backend/runtime | TimeTree Rails/Sidekiq historical; Splitwise Rails strongly indicated | no reason to replace .NET 10 |
| Relational data | TimeTree Aurora MySQL historical; Spanner migration current direction | demonstrates that product scale does not reveal a universal database choice; PostgreSQL remains suitable |
| Key/value/search | TimeTree DynamoDB/Redis historical and Spanner search direction | only relevant when HomePlatform has measured query/scale needs |
| Cloud | TimeTree AWS historical and Google Cloud migration evidence; Bring AWS | multi-cloud imitation would be counterproductive |
| Incremental sync/idempotency | Todoist API | useful protocol reference for future clients; not a mandate for an offline-first architecture now |
| Analytics/messaging SDKs | Bring names Firebase Analytics, AppsFlyer and Braze | privacy/vendor review trigger, not a core design pattern |
| CI/CD/observability | insufficient current product-specific evidence | `UNKNOWN`; do not invent |

## HomePlatform actual architecture at cutoff

Repository: repository root at `main@a87a8176676a118a2a684f02d0a2ab7f74eef182`. Existing dirty changes were preserved. No build/test command was run.

| Area | Rating | Current evidence | Required evolution |
|---|---|---|---|
| Modular monolith | `GOOD FIT` | four-project solution with direct, explainable dependencies | keep one deployable; add modules as code/features, not services |
| Project boundaries | `GOOD FIT` | Domain has no packages; Application references Domain; Infrastructure implements Application concerns; API composes | keep; strengthen tests beyond named assembly checks |
| Domain independence | `GOOD FIT` | `HomePlatform.Domain.csproj` has no package references | retain when Identity/EF arrive |
| Application use cases | `LIKELY PROBLEM` | one untracked handler is compile-broken and trusts `CreatorUserId` | trusted actor plus tested explicit use case before exposure |
| Household model | `NEEDS EVOLUTION` | owner invariant and duplicate check exist, but member identity is required UserId | stable MembershipId, optional AccountId, lifecycle and last-owner rule |
| Repository boundary | `NEEDS EVOLUTION` | aggregate-specific port and one `SaveChangesAsync` direction are reasonable | register, map and prove against PostgreSQL; no generic repository/UoW wrapper |
| EF Core | `LIKELY PROBLEM` | empty DbContext, two zero-byte configurations, no migration | explicit mappings, constraints, materialization and migration proof |
| Authentication | `LIKELY PROBLEM` | absent; current command carries creator ID | follow existing Identity/trusted-current-user roadmap |
| Authorization | `LIKELY PROBLEM` | no resource authorization; role matrix is only planning | load membership from database for every HouseholdId; fail closed |
| Transactions | `NEEDS EVOLUTION` | single SaveChanges intent only | one use-case commit; add multi-aggregate orchestration only when needed |
| Concurrency | `LIKELY PROBLEM` | in-memory `Any` is not a race guard | database uniqueness and conditional updates per invariant |
| Idempotency | `NEEDS EVOLUTION` | absent | required for invite acceptance, recurrence, sync and durable delivery when those exist |
| Scheduling/recurrence | `NEEDS EVOLUTION` | well-bounded plan, no code | limited local-time recurrence, occurrence uniqueness, DST tests |
| Notifications | `NEEDS EVOLUTION` | outside implemented and six-month core | specify preferences/dedup/quiet hours before Phase 4; infrastructure later |
| Realtime | `TOO EARLY TO KNOW` | absent | refetch/polling plus correct concurrency first; measure need for sub-second updates |
| Calendar integrations | `TOO EARLY TO KNOW` | explicitly deferred | early design spike; adapters and background sync only after validation |
| Event handling | `TOO EARLY TO KNOW` | no Domain/Application events and no multi-reaction workflow exists | use explicit orchestration; add events only when one fact has independent reactions |
| Read composition | `GOOD FIT` | Today is planned as Application query, not aggregate | keep no-tracking projection; avoid materialization until measured |
| Caching | `TOO EARLY TO KNOW` | no product query or load evidence | no Redis/distributed cache now; overengineering table marks it not justified at present |

## Current source-level blocker

`Household` now requires `(name, owner)` and validates the Owner (`backend/src/HomePlatform.Domain/Household/Household.cs:17-43`), while the handler and ten Domain-test call sites still use the one-argument constructor. The current source therefore contains a deterministic compile blocker. The earlier masterplan Parent/Child-vocabulary blocker is stale. Status: **source inspection confirmed; build NOT RUN**.

## Overengineering decision table

| Construct/technology | Timing | Business trigger |
|---|---|---|
| Explicit commands/queries/handlers | `REQUIRED NOW` | authenticated actor and one mutation/query contract per use case |
| Aggregate-specific repositories or use-case ports | `REQUIRED NOW` | load/save the consistency boundary or execute a focused query |
| Database unique/check constraints | `REQUIRED NOW` | duplicate membership, one active list, last-owner/race-related facts |
| Optimistic/conditional concurrency | `REQUIRED SOON` | demonstrated lost-update or competing owner/member/task mutations |
| Simple read models/projections | `REQUIRED SOON` | Today spans modules without cross-module writes |
| Recurrence value objects | `REQUIRED SOON` | daily/weekday local-time behavior and series semantics |
| Scheduled background job | `REQUIRED SOON` only with unattended delivery/materialization | reminder/occurrence must happen without a request |
| Domain Events | `JUSTIFIED LATER` | one committed domain fact has several independent reactions |
| Same-database outbox | `JUSTIFIED LATER` | crash-safe coupling between commit and durable delivery is required |
| External-calendar anti-corruption layer | `JUSTIFIED LATER` | first provider sync is approved after the design spike |
| SignalR/WebSockets | `JUSTIFIED LATER` | measured requirement for sub-second visibility and refetch/polling fails |
| Redis/distributed cache | `NOT JUSTIFIED` | no measured hot-read bottleneck or invalidation design |
| Queue/broker | `NOT JUSTIFIED` | no multi-service reliability/throughput trigger |
| CQRS framework/MediatR | `NOT JUSTIFIED` | direct handlers remain clearer; no pipeline repetition trigger |
| Generic repository | `NOT JUSTIFIED` | hides aggregate/use-case semantics |
| UnitOfWork wrapper | `NOT JUSTIFIED` | EF DbContext already supplies commit semantics |
| Event sourcing | `NOT JUSTIFIED` | no rebuild/audit requirement for the ordinary household domain |
| Microservices | `NOT JUSTIFIED` | one product/team, strong transactions, no independent scaling/deployment boundary |

## Security and privacy comparison

`Y/P/U` below means source-backed, partial/qualified, or unknown from the reviewed evidence. A vendor claim is not independent assurance.

| Product | Authentication model | MFA | Child/privacy model | Invite/revoke | Deletion | Export/portability | Encryption claim | GDPR/privacy rights | Incident policy | Sources |
|---|---|---|---|---|---|---|---|---|---|---|
| Cozi | shared family password | U | P: under-16 restriction | P: removal via password change | Y | P: copy/access right | U: wording too vague for E2EE | P | U | S002, S003 |
| FamilyWall | individual/circle accounts | U | Y: credentialed child account without email | P | Y | Y: portability right | P: general safeguards | Y | U | S007, S008, S088 |
| Family Tools | Parent/Standard/Linked | U | Y: loginless Linked child | P through parent/roles | P: request disclosed | U | P: transit store disclosure | P | U | S011, S012, S014 |
| Jam | individual family roles | U | Y: younger profile/caregiver scope | P | U | U | U | U | U | S015–S017 |
| TimeTree | individual account/calendar membership | U | U | Y: Creator removal/member leave | P: shared-calendar deletion; account deletion U | U | Y: transit and at-rest vendor claim | P | P: governance/audits, not a public incident process | S020, S022, S092, S093 |
| Any.do | individual account + shared space | P: new-session verification code | P: under-18 guardian involvement, no child entity | Y through space/board roles | Y | Y: copy | P: transit claim | Y | U | S027, S028 |
| Ohai | individual/duo/group plans | U | U | U | U | U | U | U | U | S030, S076 |
| Sweepy | account + household invite | U | P: child approval/profile | P | P: deletion request | U | P: transit store disclosure | P | U | S032, S033 |
| Skylight | account + profiles/devices | U | Y: child/person profiles | P | Y/P | Y: portability right | P: encrypted media protocol | Y | U | S034, S037 |
| OurFamilyWizard | separate linked parent accounts | U: support code/PIN is not MFA | Y: restricted child/third party | Y | P: legally/shared constrained | Y: record export | Y: vendor TLS/TDE claim | Y/P: erasure exceptions | U | S039–S041, S073 |
| AppClose | individual account + circles | Y: vendor 2FA claim | Y: child/third-party/caregiver scopes | Y | P: shared records may remain | Y: records/export | Y: vendor transit/at-rest claim | P | U | S042–S044 |
| AnyList | account/email household sharing | U | U | P | U | U | U | U | U | S045–S047 |
| Splitwise | individual account + groups | U | U | P | P: shared non-personal records remain | Y: CSV/JSON tiers | P: generic safeguards | P | U | S048–S051 |
| Dwellin | Primary/Sub-Account Holder; account holders 18+ | U | U: child-profile model not established | P: home invite/sub-account | U | U | U | U | U | S052, S053, S091 |
| Google Family | individual and supervised child Accounts | U in scoped sources | Y: supervised child Account, not loginless member | Y: manager/group lifecycle | U in scoped sources | U in scoped sources | U in scoped sources | U in scoped sources | U | S054, S055, S085 |

No public page was treated as proof of live MFA, encryption, deletion or incident-response controls. `U` means not established in this audit, not absent from the service.

| Topic | Competitor evidence | HomePlatform implication |
|---|---|---|
| Credentials | Cozi officially shares one password [S002]; AppClose claims 2FA [S042]; Todoist documents optional 2FA [S072]; many are `UNKNOWN` | keep individual accounts; trusted server actor; never use household credentials |
| Child identity | Family Tools and Jam directly support profiles without ordinary login; Skylight is a profile analogue. FamilyWall provides a no-email but credentialed child account; OFW credentials remain unclear; Google supports supervised child Accounts [S011, S016, S034, S040, S054, S088] | membership need not require an Account from birth; no-email and loginless are distinct patterns |
| Revocation | Cozi removal requires password reset [S002]; specialist apps have explicit linked-account/circle removal [S040, S043] | separate member removal, account deletion, household deletion and access revocation |
| Location | FamilyWall/AppClose/OFW collect or expose opt-in location with different controls [S008, S041, S044] | P3; separate high-risk context and privacy/safety review |
| Deletion/export | Maple shutdown makes portability material; Splitwise/OFW/AppClose show shared-record tension [S041, S043, S051, S056] | define subject export, household export, deletion and retained attribution before public beta |
| Encryption claims | specificity varies widely and is usually vendor-asserted | document exact at-rest/in-transit controls; do not use vague “secure” claims |
| GDPR | several publish access/erasure/portability language | not legal proof; maintain data inventory, ownership, retention and processor review |

Elevated data categories include child profile, relationships, household membership, calendar, tasks, school/pickup, location, documents, shopping and expenses. Location, school/child data, external calendar contents, payments and cross-household sharing require later legal/privacy review. This is not legal advice.

## Final architecture inference

The market does not justify replacing .NET, PostgreSQL or the modular monolith. It justifies a more defensible membership identity, correct resource authorization, recurrence/time semantics, database concurrency, explicit integration boundaries and a gradual reliable-delivery path. The most useful competitor technical pattern is Todoist's public sync/idempotency contract; the most useful negative pattern is assuming that a shared credential or realtime transport can substitute for authorization and concurrency.
