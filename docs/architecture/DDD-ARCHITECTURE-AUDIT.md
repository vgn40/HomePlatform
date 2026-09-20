# HomePlatform DDD and Clean Architecture Audit

Date: **2026-09-06**

Status: **Historical documentation audit; registration findings resolved in follow-up below**

Inspected baseline: **main@584bcaaff1b4db33481e60ed4d95bfc0ffca325e plus existing uncommitted Account Registration work**

## Current-state pointer — 2026-09-19

Historical file locations are retained as text where later moves broke links.
The historical audit and follow-up below retain their original evidence and
scope. They do not describe current main. Source inspection at `main@1ca1dd0`
confirms registration, bearer sign-in, refresh, the Account FK and
TransferOwnership/LeaveHousehold/CloseHousehold through Testing-only routes.
Current-Account validity and Household optimistic concurrency remain absent;
DeleteAccount work is uncommitted. This documentation review did not rerun
runtime tests or verify hosted CI/deployment. Read [NEXT-STEPS](roadmap/NEXT-STEPS.md)
for current status and the sole execution order.

## Registration completion follow-up — 2026-09-07

Account Registration is implemented and committed in `e2fca98`. The verified
registration baseline is 72/72 tests: Domain 25, Application 11, Integration 36.
The original audit below remains a **historical 2026-09-06 snapshot**: its code
locations, 56-test results, probes, open-status wording, and recommended order
refer to the pre-fix source, not current HEAD. They are retained as evidence.
Current implementation order is owned by [NEXT-STEPS](roadmap/NEXT-STEPS.md).

- **F01 resolved:** RegisterAccountValidator checks required input, email format
  and the 254-character bound. HTTP regression tests cover controlled 400 and
  zero writes for missing/null/malformed/oversized inputs. Identity owns strength.
- **F02 resolved for the current registration route:** UserName remains equal
  to trimmed email; the existing unique UserNameIndex is the database guard.
  Only its PostgreSQL UniqueViolation maps to EmailAlreadyExists. A deterministic
  HTTP/Application/UserManager/EF/PostgreSQL barrier test proves one 201, one 400,
  and one Account. Unrelated unique/check violations are tested as safe 500.
  RequireUniqueEmail and an independent unique EmailIndex are not implemented;
  broader email/username lifecycle policy remains in the security roadmap.
- **F03 resolved:** Application-owned error codes replace Identity descriptions;
  API emits ProblemDetails, RegisterAccountResponse, and generated OpenAPI
  201/400 metadata. HTTP and generated-document tests cover those contracts.
  Cross-endpoint error consistency and auth enumeration policy remain follow-ups.
- The handler now validates before delegation, AccountRegistrationResult uses
  private construction and named factories, and Application DI also registers
  the validator. These supersede the corresponding historical type/DI findings.
- **F04 remains open. F05 is resolved in documentation. F06 remains open:**
  pre-entry cancellation is checked, but in-flight abort/zero-write guarantees
  are not established by the registration suite.

Registration completion does not complete real sign-in/session authentication,
email confirmation, recovery, revocation, Household authorization, or production
release gates. The historical 56-test and Phase 1 57-test results are preserved;
neither is presented as the current 72-test baseline. ADR history is unchanged.

## Historical audit — 2026-09-06

## Executive summary

**Keep the current architecture.** HomePlatform has correct project dependencies,
useful dependency inversion, and meaningful tactical DDD in a small Household
aggregate. The strongest design decisions are separate Account and Membership
identity, aggregate-controlled member creation, and focused persistence with
real PostgreSQL proof. A repository or handler does not need a generic framework
to make that design valid.

This is a layered monolith with one small product-domain model and an
Infrastructure-owned Identity capability. It is a sound foundation for the
accepted modular-monolith direction; multiple implemented bounded contexts,
strategic DDD, or an event-driven system are not demonstrated. Four assemblies,
commands, DTOs, DI, and async methods are not additional DDD achievements.

The live solution builds with **0 warnings and 0 errors**. **56/56 existing tests
pass: Domain 25, Application 8, Integration 23**, including both dependency
tests. EF reports no pending model changes. The green suite does not establish
registration correctness: additional isolated HTTP/PostgreSQL probes reproduced
invalid email persistence, input-triggered 500s, a duplicate-registration race
returning 201/500, and incorrect registration OpenAPI metadata.

No Critical or High architectural finding was established. The highest-priority
findings are Medium boundary/verification gaps in the in-progress registration
slice, incomplete dependency guards, and stale authoritative documentation.
The documentation contradictions were corrected; production and test code were
not changed. Source-level Production route mapping is not evidence of deployment.

## Scope and evidence discipline

The audit began with git status, git diff, and the last 12 commits. The staged
diff was empty. Existing modified files were Directory.Packages.props,
Api/Program.cs, the Application project, and Infrastructure/DependencyInjection.cs.
Existing untracked work included API Accounts, Application RegisterAccount and
DependencyInjection, IdentityAccountRegistration, and RegisterAccountEndpointTests.
All were included in the audit rather than judged only from HEAD.

The baseline recorded SHA-256 contents for 116 tracked/non-ignored files,
including the untracked registration work. Final comparison found no added,
removed, or changed non-documentation source/configuration/test files. Generated
ignored bin/obj outputs from the requested verification are not source changes.
No reset, stash, branch switch, stage, commit, or push was performed.

Evidence labels used here:

- **Source-confirmed:** observable implementation or configuration, with locations.
- **Verified:** an identified build, test, or isolated runtime probe executed.
- **Planned:** intended behavior not present in the inspected code.
- **Not verified:** evidence was not collected, including hosted CI, deployment,
  production credentials/authentication, load, and a fresh vulnerability scan.

All active architecture documents were read: the three target documents, four
roadmap/security/debt documents, ADRs 0001–0006 and index, both architecture
research documents, and navigation/developer READMEs. Product scope was checked
for conflicting implementation claims. The documentation map, archive banners,
archived target/context proposals, and relevant archived architecture/roadmap
sections were consulted as historical evidence. Competitor factual claims and
external market research were not re-audited or used to establish current code.

## Repository and dependency map

### Actual solution

[HomePlatform.slnx](../../backend/HomePlatform.slnx) contains four production
projects and three test projects. All target net10.0 through
[Directory.Build.props](../../backend/Directory.Build.props), with nullable
references, implicit usings, and warnings as errors. global.json selects SDK
10.0.400 with latestFeature roll-forward. Packages are centrally versioned in
[Directory.Packages.props](../../backend/Directory.Packages.props).

Actual direct project references, not runtime call direction:

```text
Domain         -> none
Application    -> Domain
Infrastructure -> Application + Domain
Api            -> Application + Infrastructure

Domain.Tests      -> Domain
Application.Tests -> Application + Domain
IntegrationTests  -> Api + Domain + Infrastructure
```

All production references match the intended graph. IntegrationTests can access
Application transitively through API; the missing direct reference is not an
architecture violation. API likewise receives Domain and technical libraries
transitively; absence of a direct project reference is not proof that an outer
layer cannot use a transitive type.

| Project | Direct packages / framework | Assessment |
|---|---|---|
| [Domain](../../backend/src/HomePlatform.Domain/HomePlatform.Domain.csproj) | no packages; Microsoft.NETCore.App | PASS: no ASP.NET, EF, Npgsql, Identity, DI, or outer project dependency |
| [Application](../../backend/src/HomePlatform.Application/HomePlatform.Application.csproj) | DependencyInjection.Abstractions 10.0.11; Microsoft.NETCore.App | PASS for pragmatic Clean Architecture: only composition helper uses DI; no HTTP, EF, Identity, or Infrastructure |
| [Infrastructure](../../backend/src/HomePlatform.Infrastructure/HomePlatform.Infrastructure.csproj) | EF Core Design 10.0.11, Npgsql EF provider 10.0.3, Identity EF 10.0.11 | Correct owner of EF, PostgreSQL and credential persistence |
| [API](../../backend/src/HomePlatform.Api/HomePlatform.Api.csproj) | ASP.NET Core shared framework, OpenApi 10.0.11, EF Design 10.0.11 | Composition root and migration startup project; EF Design is PrivateAssets=all, not a Domain dependency |
| Domain/Application tests | Test SDK 17.14.1, xUnit 2.9.3, runner 3.1.4 | Appropriate unit/dependency-test dependencies |
| IntegrationTests | same test stack, Mvc.Testing 10.0.11, Testcontainers.PostgreSql 4.14.0 | Appropriate HTTP/DI/PostgreSQL/Identity proof |

Resolved assets were also inspected. Domain has no package graph. Application
resolves only Domain and DI abstractions. Infrastructure resolves EF Core,
Relational, and Identity libraries at 10.0.11 and Npgsql at 10.0.3, plus framework
support and EF Design tooling dependencies such as Roslyn/Humanizer. API includes
the outer dependencies and Microsoft.OpenApi 2.7.5. Those tooling/transitive
libraries do not leak into the inner projects. This is a dependency-direction
assessment, not a fresh vulnerability certification.

### Type and component inventory

Paths below are relative to backend/src unless otherwise stated.

| Area | Types / artifacts | Actual responsibility |
|---|---|---|
| Domain/Household | Household, HouseholdMember, HouseholdRole | member creation and Household invariants; enum vocabulary |
| Domain/common | Result | small success/failure helper |
| Application/Households/CreateHousehold | command, handler, result, outcome enum | create use case, expected outcomes, durable-write orchestration |
| Application/Households | IHouseholdRepository | aggregate-specific persistence port with AddAsync |
| Application/Identity | ICurrentAccount | AccountId actor port |
| Application/Accounts | IAccountRegistration, AccountRegistrationResult | credential-registration port and framework-neutral result type |
| Application/Accounts/RegisterAccount | command, handler | input data and direct use-case delegation |
| Infrastructure/Identity | ApplicationUser, IdentityAccountRegistration | Identity model and UserManager adapter |
| Infrastructure/Persistence | HomePlatformDbContext, two configurations, HouseholdRepository | roleless Identity/product model, private-state mapping, aggregate commit |
| Infrastructure/Persistence/Migrations | three migrations, designers, one snapshot | ordered Household/name-bound/Identity schema evolution |
| Api/Households | request, response, endpoints | Name-only request and 201/400/401 mapping |
| Api/Accounts | RegisterAccountRequest, AccountEndpoints | separate HTTP input; anonymous success/error objects |
| Api/Identity | HttpCurrentAccount | authenticated ClaimsPrincipal to Guid adapter |
| Api root | Program, HealthEndpoints | composition, middleware, environment routing, operational probes |
| Application/Infrastructure root | DependencyInjection | layer-owned registration helpers |

There are no explicit value-object types, Domain Services, Domain Events,
Integration Events, generic repositories, UnitOfWork wrappers, event dispatchers,
MediatR handlers, read-model implementations, or additional product aggregates.

## DDD findings and maturity

| Classification | Type | Reason / limit |
|---|---|---|
| Entity and Aggregate Root | Household | stable Id, owns the member set, establishes initial Owner, controls additions and scoped duplicate links |
| Aggregate | Household plus HouseholdMember entities | small consistency boundary; root creation persists the whole graph in one commit |
| Child Entity | HouseholdMember | MembershipId establishes participation identity independently of optional AccountId; owned and created through Household |
| Invariants | Household/member constructors and AddMember | normalized bounded name, nonempty linked IDs, valid role, Account-linked Owner, no duplicate non-null Account in one Household |
| Enum | HouseholdRole | Owner/Member/Guest vocabulary; not a value object, service, or authorization implementation |
| Ordinary result class | Result | success/failure data, no identity or domain value behavior; not a DDD pattern by location |
| Application service | CreateHouseholdHandler | obtains actor, constructs aggregate, translates expected errors, calls persistence, returns data |
| Application service | RegisterAccountHandler | delegates to registration port; useful boundary despite no present orchestration beyond forwarding |
| Port and repository abstraction | IHouseholdRepository | speaks Household aggregate persistence, not child-table CRUD; creation-only API is sufficient today |
| Ports | ICurrentAccount, IAccountRegistration | isolate host identity and credential infrastructure; ports are architectural boundaries, not Domain Services |
| Adapters | HttpCurrentAccount, IdentityAccountRegistration, HouseholdRepository | implement ports using HTTP principal, Identity, and EF respectively |
| DTOs/input/result records | commands, requests, responses, Application results | data contracts; record syntax does not make them domain value objects |
| Framework entity | ApplicationUser | Identity persistence model; not a newly justified product-domain Account aggregate |

Using the requested scale: Level 0 is transaction scripts, Level 1 layer
separation, Level 2 a behavioral model with dependency inversion, Level 3 explicit
aggregate ownership across meaningful behavior, and Level 4 strategic modelling
across independently owned contexts. Distribution is a separate topology decision;
it is not a higher-quality DDD goal in itself.

**Assessment: Level 2 overall, with early Level 3 in Household.** The explicit
root/child boundary is real, but the model still has only creation and addition;
authorization, linking and lifecycle mutations are missing. Do not describe this
as multiple mature bounded contexts or distributed strategic DDD. Do not add
patterns to increase a score.

## Household aggregate analysis

[Household.cs](../../backend/src/HomePlatform.Domain/Household/Household.cs)
lines 9–78 and [HouseholdMember.cs](../../backend/src/HomePlatform.Domain/Household/HouseholdMember.cs)
lines 5–37 establish a genuine root/child relationship:

- Household's public constructor rejects blank or over-100-character normalized
  names and empty owner AccountId. It generates identity/timestamps and one
  distinct Account-linked Owner membership.
- The member constructor is internal; external assemblies cannot construct it
  normally. MembershipId and Role are getter-only; AccountId has a private setter.
  There is no public member mutation, linking, removal, or role-change API.
- AddMember checks duplicate non-null AccountId before mutation and delegates
  valid-role/nonempty-account/Owner-account checks to the child constructor.
  Rejected duplicates and thrown constructor validation leave the collection
  unchanged. Invalid role/empty Account input paths exist in code but are not
  separately covered by the current tests.
- Members returns List.AsReadOnly, a live read-only wrapper. It is not the
  backing List and does not permit mutation through ICollection. Existing
  references observe later additions. A new small wrapper per getter access is
  not a demonstrated performance problem.
- Household can have multiple linked Owners, and several loginless Member/Guest
  entities. The current API cannot remove the initial Owner. Absence of
  last-Owner race handling is a future mutation gate, not an observed current
  path that removes the last Owner.

The private parameterless constructors exist for materialization and are not a
public invalid-state escape. No EF attributes, navigation base classes,
Identity fields, HTTP types, or database APIs occur in Domain. Constructor
validation does not run again when EF materializes stored data; persistence
ignorance is not protection against arbitrary bad database writes.

[HouseholdConfiguration](../../backend/src/HomePlatform.Infrastructure/Persistence/Configurations/HouseholdConfiguration.cs)
lines 12–34 maps generated IDs with ValueGeneratedNever, bounded required Name,
UTC DateTime columns, and a required shadow HouseholdId relationship with cascade
delete. Navigation field access uses the conventional `_members` field.
[HouseholdMemberConfiguration](../../backend/src/HomePlatform.Infrastructure/Persistence/Configurations/HouseholdMemberConfiguration.cs)
lines 13–30 maps MembershipId, optional AccountId, required integer Role, and a
unique unfiltered index on HouseholdId/AccountId. Reload tests show that the
private state is materialized compatibly with encapsulation.

| Rule | Domain protection | Database / current proof |
|---|---|---|
| Nonblank, trimmed, bounded name | constructor | NOT NULL and varchar(100); no nonblank/trim CHECK |
| Stable MembershipId | generated getter-only identity | primary key/ValueGeneratedNever and reload assertions; no link/unlink proof yet |
| Non-null AccountId must be nonempty | constructor | uuid column; no empty-Guid check |
| Valid role and linked Owner | member constructor | integer/nullable columns; no enum or Owner-account CHECK |
| Scoped linked-account uniqueness | AddMember | unique index rejects a stale duplicate; multiple null links are allowed |
| Valid Account reference | Guid shape only | no FK to AspNetUsers or Application validation; accepted follow-up |
| At least one Owner | enforced at creation; no removal API | no database last-Owner constraint or aggregate version token |
| UpdatedAt | set from UtcNow on addition | mapped timestamp; not a concurrency token or guaranteed strictly increasing clock |

The repository boundary is correctly around Household. Its AddAsync tracks the
whole graph and calls SaveChangesAsync once. Existing real PostgreSQL tests prove
one async save invocation, cancellation before writing, and full rollback after
a forced child insert failure with the parent already inserted in the transaction.
EF's transactional SaveChanges default supports this design, and the local
tests supply the project-specific evidence.
[EF transaction documentation](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

There is no production child repository or child CRUD endpoint. Direct DbContext
member access in verification tests is not product leakage. For later mutations,
load the member set needed for aggregate invariants and keep mutations through
Household; do not reinterpret the public EF Set API as an Application contract.
Adding Tasks, lists, events, or credential state to this aggregate is not justified.

No value object is necessary for current name/ID behavior: name normalization is
centralized and identifiers have few call sites. Consider HouseholdName only if
behavior repeats across real mutations, typed IDs after concrete wrong-ID errors,
and recurrence/time value objects when that domain behavior arrives. Do not add
a clock merely to make timestamps look more sophisticated; introduce controlled
time when expiration or recurrence has a testable business meaning.

## Account and Identity boundary

The former Domain User has been removed. The live boundary is:

```text
HTTP email/password -> RegisterAccountRequest
                    -> RegisterAccountCommand / RegisterAccountHandler
                    -> IAccountRegistration
                    -> IdentityAccountRegistration / UserManager<ApplicationUser>
                    -> Identity EF store / AspNetUsers
                    -> Application AccountRegistrationResult
                    -> HTTP result

Authenticated principal -> API HttpCurrentAccount -> Application ICurrentAccount
                        -> Household stores Guid AccountId on a Membership
```

[ApplicationUser](../../backend/src/HomePlatform.Infrastructure/Identity/ApplicationUser.cs)
is a sealed `IdentityUser<Guid>` in Infrastructure. DbContext derives from
`IdentityUserContext<ApplicationUser, Guid>`, calls base.OnModelCreating first,
then applies product mappings. The migration has Users, Claims, Logins, and
Tokens; no global role tables are present. HouseholdRole is neither an Identity
role nor a long-lived claim. This separation is appropriate.

Application's registration port carries transient email/password input, which
is acceptable for the registration use case. It exposes no IdentityResult,
IdentityError, UserManager, ApplicationUser, ClaimsPrincipal, or DbContext.
Infrastructure translates framework result *types*, but currently forwards
framework error descriptions unchanged. That is semantic coupling at the public
contract, not an assembly dependency violation (F03).

**Do not invent an Account aggregate now.** There are no independent product
Account invariants beyond framework credential management. An Account Guid in
Domain does not need a Domain Account entity. Profile behaviour could justify a
separate model later; it does not justify duplicating credentials today.

Registration creates users but does not authenticate subsequent requests.
AddIdentityCore/AddEntityFrameworkStores do not establish a login endpoint or
cookie/bearer scheme. No SignInManager setup, confirmation/reset provider flow,
or session/recovery API exists. Keep the real-authentication gate explicit.
Identity's first-party bearer option remains a reasonable candidate, with the
documented simple-client limitations; it is not a general OAuth/OIDC server.
[Microsoft Identity API guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0).

Account existence is also a separate boundary. Household creation tests use
random trusted test AccountIds without matching AspNetUsers rows. This is a
known test-slice shortcut, not evidence of referential integrity. Before real
Household writes/linking, choose focused Account validation and an explicit
deletion/integrity strategy. A direct database FK is an Infrastructure decision,
not inherently a Domain dependency violation; evaluate its lifecycle consequences
and existing rows instead of blindly adding or banning it.

## Layer-by-layer findings

### Domain

Business invariants are in constructors/AddMember, state is encapsulated, and
framework isolation is strong. Result is a small utility with string errors,
not a DDD framework. AddMember mixes a Result for expected duplicate links with
ArgumentException for invalid construction. This does not corrupt state; define
stable expected outcomes at the next exposed membership use-case boundary rather
than refactoring every constructor during this audit.

### Application

[CreateHouseholdHandler](../../backend/src/HomePlatform.Application/Households/CreateHousehold/CreateHouseholdHandler.cs)
is an orchestrator. It gets actor identity from the port, catches the documented
unauthenticated outcome narrowly, constructs Domain, maps argument validation,
passes cancellation to the repository, and returns only after the commit.
Repository failures and programming errors are not swallowed.

`backend/src/HomePlatform.Application/Accounts/RegisterAccount/RegisterAccountHandler.cs` (historical path)
lines 8–15 is only a forwarding use-case boundary. Keeping it is reasonable for
consistent composition and future policy; no handler interface, mediator,
pipeline framework, or Domain Service is needed to justify eight lines of
delegation. Do not add a fake business rule solely to enrich it.

Commands are input records, and Application results are framework-neutral.
CreateHouseholdResult uses named factories/private construction;
AccountRegistrationResult is a public positional record, so other callers could
construct success with no AccountId or inconsistent Errors. The current adapter
returns coherent combinations; this is a minor contract-strength issue rather
than an observed invalid current response. Narrow named outcomes/factories can
be considered while implementing the real registration error contract.

### Infrastructure

Ownership is correct: EF, mappings, migrations, Identity, and repository
implementation are outside Domain/Application. HouseholdRepository.AddAsync
includes the durable save. There is no extra UnitOfWork, IQueryable port, generic
repository, or duplicated identity model.

Both persistence adapters use scoped services. SaveChanges commits everything
tracked in that scoped context, so future handlers must not compose several
auto-committing adapter calls and assume all-or-nothing semantics. No current
handler does so. Invitation acceptance can use a focused transaction over both
aggregates; registration plus Household creation would require its own explicit
semantics if later combined.

The three migrations are sequential and focused. InitialHousehold creates root
and child; LimitHouseholdNameLength narrows Name to varchar(100);
AddIdentityPersistence adds roleless credential tables with FKs to users.
There is one snapshot, a local 10.0.11 tool pin, no model drift, and no startup
Database.Migrate/EnsureCreated in Program. The historical length-narrowing
migration needs a data preflight if ever applied to existing overlong names;
fresh-database tests do not prove that upgrade scenario. Do not rewrite already
applied migration history during future fixes.

Database constraints are accurately limited in the aggregate table above.
Identity's NormalizedUserName index is unique; NormalizedEmail's EmailIndex is
not. UserName=email.Trim means the present route still prevents duplicate
normalized usernames, including the forced race. It does not provide the
documented independent email policy or controlled race result (F02).

### API

Product endpoints map separate request DTOs into commands and Application
results into HTTP. No Domain/EF/Identity entity is serialized. Household has an
explicit response record; registration's anonymous success/error objects are
still API-owned DTO shapes, although explicit types and metadata would make
their contract more reviewable.

[HttpCurrentAccount](../../backend/src/HomePlatform.Api/Identity/HttpCurrentAccount.cs)
checks IsAuthenticated, prefers NameIdentifier then sub, and rejects missing,
malformed, or empty Guid values. A malformed preferred claim does not fall through
to a valid alternate claim. Input AccountId cannot choose the actor: the Household
DTO and command contain only Name, and the HTTP impersonation test verifies it.
The fake X-Test-Account-Id scheme exists only in the integration-test assembly.
No real credential/token authentication is claimed.

Program maps registration anonymously in **all environments**, including
Production, while Household is mapped only in Testing. Anonymous registration
is normal for a registration endpoint; exposing an unfinished contract is a
separate release decision. The isolated Production-environment probe returned
201 for registration. That host still used the test factory's services and is
route-exposure evidence only; it does not prove production authentication or
deployment.

Household expected outcomes map to 201/400/401; unexpected exceptions map to
generic ProblemDetails 500. The name-validation detail currently contains an
ArgumentException message, including a parameter label. Registration expected
failures use `{ errors: string[] }` with Identity descriptions. The global
exception middleware does not normalize those explicit 400 responses. Public
codes, response shape and registration OpenAPI need alignment (F03).

[HealthEndpoints](../../backend/src/HomePlatform.Api/HealthEndpoints.cs) lines
14–21 directly depend on DbContext/EF for a connectivity probe. This is real
Infrastructure coupling outside Program, but it is a small operational adapter,
not a demonstrated business-data-access problem. Document it as an exception;
do not invent a Domain/Application readiness use case. A standard health-check
adapter may become useful when more operational dependencies exist. Readiness
proves connectivity, not migration/schema readiness. Under a future fallback
authorization policy, health routes need explicit anonymous metadata; the current
Program has no fallback policy and those routes have no AllowAnonymous marker.

## Dependency Injection composition

```text
Api / Program
  AddApplication
    scoped RegisterAccountHandler -> IAccountRegistration
    scoped CreateHouseholdHandler -> IHouseholdRepository + ICurrentAccount
  AddInfrastructure
    scoped HomePlatformDbContext -> Npgsql connection
    scoped IHouseholdRepository -> HouseholdRepository -> same scoped DbContext
    Identity Core -> scoped UserManager/ApplicationUser store -> scoped DbContext
    scoped IAccountRegistration -> IdentityAccountRegistration -> UserManager
  AddHttpContextAccessor
  scoped ICurrentAccount -> HttpCurrentAccount -> IHttpContextAccessor
  AddAuthentication / AddAuthorization
  AddProblemDetails / AddOpenApi
```

Application DI registers only Application handlers. Infrastructure DI registers
Infrastructure implementations plus the framework services they require.
Domain contains no DI. Program is the composition root. There are no duplicate
production handler/port registrations or singleton-to-scoped captures in the
inspected code. IHttpContextAccessor's framework lifetime is appropriate; it
does not make a scoped actor service global.

The full HTTP tests successfully resolve both handler paths and their EF/Identity
dependencies. The test factory overrides authentication and, in one error test,
the repository; those are intentional test seams, not duplicate production DI.
ConnectionStrings:Database is required and checked at registration. No default
production authentication scheme exists to resolve/challenge until the next
Identity work. The small DI abstraction package in Application is an explicit
pragmatic exception to total framework independence; it does not justify moving
Identity registration or EF into Application.

## Test architecture and verification coverage

| Level | Current result | What it actually proves |
|---|---|---|
| Domain.Tests | 25/25 PASS | constructor/name/owner behavior, member creation/duplicates/loginless members, read-only view, basic Result, named dependency check |
| Application.Tests | 8/8 PASS | CreateHousehold orchestration with fakes, expected outcomes, actor use, token forwarding, zero writes, exception propagation, named dependency check |
| IntegrationTests | 23/23 PASS | HTTP/DI, PostgreSQL persistence, migrations in endpoint fixtures, Identity password persistence, health, generated Household OpenAPI and generic error non-disclosure |

PostgreSQL 18.6-alpine Testcontainers is appropriate for actual provider
constraints, private-state materialization, transaction rollback and Identity
stores. Each IAsyncLifetime test instance starts its own container, so even
same-named databases are isolated per test. Test methods do not depend on order
or a shared development database. CreateHousehold's extra table reset is
redundant with the current per-test fixture, but harmless. Container-per-test
cost is acceptable at this size; optimize only if measured runtime warrants it.

HouseholdPersistenceTests uses **EnsureCreated**, while CreateHousehold and
RegisterAccount endpoint fixtures use **MigrateAsync**. Consequently persistence
tests prove the EF model and repository, and endpoint tests prove the migration
chain from zero. Do not attribute migration proof to EnsureCreated. Migration
upgrade with existing production-like rows is not covered.

The stale-aggregate duplicate test uses two independently loaded contexts and
sequential commits. It proves database uniqueness against stale snapshots, not
all simultaneous owner/linking races. The one-save interceptor test is useful
because durable commit semantics are an explicit repository contract; it is not
merely a mock mirroring a private method. Reflection on the exact collection
type and two trivial Result factory tests have lower value, but their existence
does not warrant a cleanup project.

Both [Domain dependency test](../../backend/tests/HomePlatform.Domain.Tests/DependencyDirectionTests.cs)
and [Application dependency test](../../backend/tests/HomePlatform.Application.Tests/DependencyDirectionTests.cs)
pass. They call Assembly.GetReferencedAssemblies and reject only named outer
HomePlatform assemblies. They do not reject EF/ASP.NET/Identity packages, unused
ProjectReferences, FrameworkReferences, Infrastructure-to-API dependencies, or
future cross-module references. Green tests are partial boundary protection (F04).

Meaningful coverage gaps for subsequent code tasks:

- registration malformed/null/bounded inputs, stable error codes, generated
  OpenAPI and concurrent duplicates; the added audit probes are not regression
  tests committed to the repository;
- HttpCurrentAccount's authenticated-but-missing/malformed claims and sub fallback:
  current malformed-header tests fail in TestAuthenticationHandler first;
- migration upgrades preserving existing Household data when Account-reference
  integrity changes; a clean migration and a pending-model check are different;
- real authentication/challenge, Account validation, Membership authorization,
  and deterministic mutation races when those use cases are implemented.

A RegisterAccount forwarding-only unit test is optional and low value today;
invest in behavior at the registration port/HTTP/persistence boundaries instead.

## Additional isolated runtime evidence

The audit used unchanged freshly built production/test assemblies from this
working tree, a separate verification program outside the repository, and a new
disposable PostgreSQL database migrated from zero. No test/source files were
added to HomePlatform. The initial probe launch failed to locate the solution
from the external directory; setting its test content root corrected the harness,
after which the checks below completed. That harness setup failure was not a
product failure.

| Probe | Observed result | Interpretation |
|---|---|---|
| Valid email and valid password | 201; one user persisted | baseline registration works |
| `email: "not-an-email"` and valid password | 201; one user persisted | invalid email is accepted (F01) |
| Null email / omitted email | 500 ProblemDetails; zero new rows | missing-input validation gap (F01) |
| Null password | 500 ProblemDetails; zero new rows | expected input error becomes server error (F01) |
| 257 `a` characters followed by `@example.com` | 500 ProblemDetails; zero new rows | length is not controlled before persistence (F01) |
| Sequential duplicate valid email | 400 JSON with a framework DuplicateUserName description; no new row | sequential guard works, product error contract is framework-coupled |
| Two registrations synchronized immediately before save | 201 and 500; exactly one matching row; SQLSTATE 23505, UserNameIndex | uniqueness protects storage; duplicate race is not translated (F02) |
| Generated registration OpenAPI | only 200 response, no operationId, generic HomePlatform.Api tag | metadata disagrees with actual success/failure (F03) |
| Registration on Production-environment test host | 201 | route is mapped outside Testing; no deployment/authentication claim |
| Resolved Identity options | RequireUniqueEmail=false, RequireConfirmedEmail=false, RequiredLength=6 | default options, not the full security-roadmap policy |

The race interceptor waited until both requests reached SavingChangesAsync after
their validation queries. It then released both actual EF inserts; two arrivals,
the real database exception, status pair, and final row count were observed.
It did not replace the store, synthesize a duplicate result, or alter production
code. The input-triggered 500s returned generic ProblemDetails with traceId;
they did not reveal the internal exception in the observed response bodies.

## Target architecture review

**Verdict: retain the modular monolith and simplify the language of evolution.**
Most existing target guidance already rejects speculative enterprise tooling.
The audit did not discover a mandate to implement every fashionable pattern.
The material problems were stale current-state claims, treating feature areas
too readily as future contexts, and combining different event/delivery triggers.

| Idea | Classification | Problem, timing, cost, and simplest alternative |
|---|---|---|
| Four layers with explicit handlers and ports | Appropriate now | Already isolates business rules and credential/persistence adapters with little ceremony; keep it |
| Household aggregate / focused repository | Appropriate now | Protects real membership/owner/duplicate rules; no child CRUD abstraction needed |
| Account aggregate | Premature / unnecessary now | No independent product Account invariants; let Identity own credentials |
| Bounded contexts | Only if model/ownership diverges | Separate language, invariants, model, lifecycle and ownership must exist; begin with feature folders and contracts |
| Plain command/query separation | Reasonable with first read use case | Avoid loading aggregates for display; use same-database authorized projections |
| Read models / Today | Reasonable later | Composes display data; DTO/query first, no Today aggregate/table by default |
| Separate CQRS datastore | Only if measured read pressure requires it | Adds lag, synchronization, replay/rebuild and operational burden; indexed projections first |
| MediatR | Only if repeated pipelines justify it | Direct handlers are clear; first compose shared behavior without an extra dispatch framework |
| Domain Service | Only if a rule fits no entity/VO | No current example; do not rename a handler or utility to claim DDD |
| Domain Events | Only if one action has several independent reactions | Can decouple in-process behavior; define dispatch/transaction/error ordering before adoption |
| Integration Events / event-driven integration | Only if another module/process must react asynchronously | Needs stable contracts, retry and idempotency; ordinary explicit calls remain simpler now |
| Outbox | Only if commit plus reliable external publication/delivery must be atomic | Durable intent and retry solve the crash window; requires deduplication/backlog handling; no exactly-once promise |
| Worker/delivery table | Reasonable with first unattended reminder | Solves work outside a request; a small DB-backed worker is enough initially |
| Message bus/broker | Only if cross-process consumers/scale exceed that worker | Adds redelivery, ordering, operations, monitoring and consumer contracts; not implied by outbox/events |
| EF unit-of-work behavior | Appropriate now | One SaveChanges already commits the tracked unit atomically |
| Generic repository / UnitOfWork wrapper | Premature / unnecessary | Duplicates EF and weakens use-case semantics; focused transaction for a concrete multi-aggregate flow |
| Specification Pattern/framework | Not planned | No repeated complex predicate model exists; use focused named queries first |
| Shared Kernel | Not planned | No independently owned contexts require jointly governed shared business types; Common/Result is not strategic DDD |
| Anti-Corruption Layer | Reasonable with first calendar provider | Actual provider semantics may differ; plain mapping adapter first, no new platform |
| Sagas | Not planned | No distributed business transaction; one local transaction is simpler and stronger now |
| Event Sourcing | Premature / unnecessary | Audit/history records solve ordinary attribution; replay as authoritative state requires a much stronger need plus version/deletion/rebuild design |
| Microservices | Premature / unnecessary | No independent deployment/scaling/team requirement is evidenced; retain one deployable monolith |
| Per-module projects, schemas or DbContexts | Only if ownership/migration conflicts are measured | Four assemblies and one database suffice; premature isolation makes transactions and CI harder |
| Azure operational target | Reasonable before deployment/beta | One API and managed PostgreSQL are proportionate; validate cost/recovery at deployment time and keep Azure out of inner layers |
| Redis, realtime, Kubernetes, multi-region | Only if explicit measured triggers occur | Optimize queries/refetch first; no current workload evidence justifies these components |

The target's event-sourcing trigger previously accepted historical reconstruction
too readily. History alone does not require event sourcing. Likewise “one fact
has durable reactions” did not distinguish domain events from outbox and broker.
The updated target gives each a separate trigger and preserves a simple first step.

Security work is not automatically overengineering. Trusted actor, Account
validation, resource authorization, safe errors, and atomic ownership changes
protect existing/planned data boundaries. Key persistence, backup/restore and
operational isolation belong at the relevant deployment gate; they are not a
reason to add a distributed architecture to Domain today.

## Bounded context assessment

Households has meaningful terms and invariants now. Identity has a different
credential lifecycle and a clean supporting boundary. That is a good reason for
separation, but there is no evidence of several independently owned, evolving
product-domain models yet.

A future context should demonstrate all of: coherent ubiquitous language, own
invariants, its own model, independent lifecycle, clear ownership, and distinct
reasons to change. Document the translation where concepts differ across the
boundary. Folder names, assemblies, feature lists and database tables do not
satisfy these criteria by themselves.

Tasks, Shopping and internal Calendar could remain modules within one household
coordination context until their models diverge. Today is a query surface.
Notifications may stay an adapter until delivery/preferences have independent
model ownership. Finances and home automation are discovery candidates, not
mandatory bounded contexts or fields to add to Household. Even if real contexts
emerge, **modular monolith is preferred to microservices** until a separate
deployment/scaling/ownership requirement is evidenced.

## Findings

### F01 — Medium — Registration accepts invalid email and turns invalid input into 500

**Location:** `backend/src/HomePlatform.Api/Accounts/RegisterAccountRequest.cs` (historical path)
lines 3–5; [IdentityAccountRegistration.cs](../../backend/src/HomePlatform.Infrastructure/Identity/IdentityAccountRegistration.cs)
lines 15–26; [Infrastructure DI](../../backend/src/HomePlatform.Infrastructure/DependencyInjection.cs)
lines 32–34.

**Observation:** the request is an unvalidated string record; the adapter calls
email.Trim and UserManager.CreateAsync with default options. The isolated HTTP
checks reproduced malformed email persistence and missing/null/oversized input
500s. Resolved RequireUniqueEmail is false. The framework default and conditional
email-validation path support the runtime diagnosis.
[Identity options](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0),
[UserValidator source](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Identity/Extensions.Core/src/UserValidator.cs).

**Why it matters:** a credential-bearing public boundary accepts unusable Account
data and classifies expected client input as server failure. Nullable C# syntax
does not validate JSON. This is a concrete boundary bug, not missing DDD richness.

**Recommendation:** add narrow validation for required input, email format and
chosen length limits before framework/persistence operations. Return owned
Application validation outcomes and controlled HTTP responses. Keep password
policy/hashing in Identity. Add the reproduced cases as regression tests in a
separate code task. **Open; no code changed.**

### F02 — Medium — Concurrent duplicate registration has no controlled result

**Location:** [IdentityAccountRegistration.cs](../../backend/src/HomePlatform.Infrastructure/Identity/IdentityAccountRegistration.cs)
lines 24–35; [Identity migration](../../backend/src/HomePlatform.Infrastructure/Persistence/Migrations/20260904100319_AddIdentityPersistence.cs)
lines 111–120; `backend/tests/HomePlatform.IntegrationTests/RegisterAccountEndpointTests.cs` (historical path)
lines 76–100.

**Observation:** the migration's UserNameIndex is unique; EmailIndex is not.
The adapter sets UserName equal to trimmed email. Sequential duplicate detection
works, but two real requests synchronized before save returned 201/500, one row,
and PostgreSQL 23505 on UserNameIndex. The adapter handles IdentityResult errors,
not this database race. The existing test exercises only a sequential duplicate.

**Why it matters:** storage is protected from duplicate usernames; the defect is
the uncontrolled public race outcome and mismatch with the documented independent
normalized-email policy. Do not incorrectly report that this endpoint permits
two duplicate Accounts just because EmailIndex is nonunique.

**Recommendation:** decide/enforce the normalized-email policy, including existing
data and future email/username changes. Add the chosen database guard and map only
recognized duplicate constraints to an owned outcome. Handle the current username
constraint as applicable; do not catch every DbUpdateException or convert unknown
failures to conflicts. Prove one winner and controlled responses with a barrier
test. **Open; no migration or code changed.**

### F03 — Medium — Registration HTTP and generated OpenAPI contracts disagree

**Location:** [AccountEndpoints.cs](../../backend/src/HomePlatform.Api/Accounts/AccountEndpoints.cs)
lines 25–40; [IdentityAccountRegistration.cs](../../backend/src/HomePlatform.Infrastructure/Identity/IdentityAccountRegistration.cs)
lines 30–35; [HouseholdEndpoints.cs](../../backend/src/HomePlatform.Api/Households/HouseholdEndpoints.cs)
lines 24–56; [OpenApiContractTests](../../backend/tests/HomePlatform.IntegrationTests/OpenApiContractTests.cs).

**Observation:** registration emits 201 or JSON 400 with raw Identity descriptions;
generated OpenAPI advertises only 200. There is no declared registration response
schema or operationId. Household has explicit 201/400/401 metadata and uses
ProblemDetails for invalid input. Stable product codes are not implemented.

**Why it matters:** generated clients and error handling cannot reliably follow
the actual API. Framework descriptions become the user-visible contract and are
even asserted through IdentityErrorDescriber in tests. A framework-neutral result
type alone does not decouple error meaning or text.

**Recommendation:** map recognized Identity codes into Application-owned errors,
use a consistent public ProblemDetails/code policy, and declare the registration
success/error metadata (or use typed results with equivalent metadata). Test the
generated `/openapi/v1.json` and actual HTTP responses. Keep unexpected failures
generic. Decide account-enumeration behavior with the auth contract instead of
silently promising non-enumeration while returning duplicate identity text.
**Open; no endpoint changed.**

### F04 — Medium — Dependency tests provide narrower protection than the architecture rules

**Location:** [Domain dependency test](../../backend/tests/HomePlatform.Domain.Tests/DependencyDirectionTests.cs)
lines 10–17; [Application dependency test](../../backend/tests/HomePlatform.Application.Tests/DependencyDirectionTests.cs)
lines 10–16.

**Observation:** two passing tests blacklist only selected outer HomePlatform
assembly names from emitted references. They have no framework/package guards
or csproj/FrameworkReference inspection, and no Infrastructure direction check.

**Why it matters:** an EF or Identity dependency, or an unused forbidden project
reference, could be added while both tests stay green. No such inner-layer
dependency currently exists; this is a protection gap, not a present violation.

**Recommendation:** extend the existing small tests to inspect project metadata
and forbidden runtime/package/framework families, with an explicit allowance for
Application DI abstractions. Guard Infrastructure-to-API and meaningful API type
boundaries. Add cross-module rules only when multiple modules actually exist.
No architecture-testing package is required. **Open; tests preserved.**

### F05 — Medium — Authoritative docs described removed, completed and future work incorrectly

**Location before correction:** target architecture's Account/Membership decision
gate and persistence section; Domain Model User row; NEXT-STEPS current gate;
masterplan Phase 2; debt TD-006; security current-exposure paragraph; product scope.

**Observation:** ADR 0006 was still described as proposed/unaccepted in the target
despite its Accepted status and implemented migrations. Domain User was still
listed after removal, Phase 2 was said not to have started, and Production was
said to map only health/readiness despite live registration. Some current wording
mixed historical test/tool versions with later code and treated future link
behaviour as already implemented.

**Why it matters:** the authoritative plan could block valid work, recreate a
removed model, or mislead reviewers about release exposure and verification.

**Recommendation/action:** corrected active docs, preserved dated historical
evidence/ADRs, and separated current implementation, open gates and evolution
triggers. **Resolved in documentation only.**

### F06 — Low — Registration cancellation is checked only before starting Identity work

**Location:** `backend/src/HomePlatform.Application/Accounts/IAccountRegistration.cs` (historical path)
lines 5–8; [IdentityAccountRegistration.cs](../../backend/src/HomePlatform.Infrastructure/Identity/IdentityAccountRegistration.cs)
lines 10–26.

**Observation:** the port accepts a CancellationToken, but the adapter only calls
ThrowIfCancellationRequested before UserManager.CreateAsync. That method call
does not receive the request token. This differs from HouseholdRepository, which
passes it to SaveChangesAsync. Cancellation before entry is supported; in-flight
abort/zero-write behavior has not been established for registration.

**Why it matters:** consumers must not assume that a canceled/disconnected request
guarantees no Account was created. A committed write and lost response require
explicit outcome/retry semantics; cancellation is not a rollback protocol.

**Recommendation:** document the boundary's actual cancellation guarantee and
verify pre-cancellation. If request-abort propagation is a requirement, evaluate
the supported Identity/store integration deliberately. Do not throw after a
successful commit merely to make the operation look canceled. **Open; low priority.**

## Documentation correctness and changes

| Previous claim / classification | Correct current statement | Changed document |
|---|---|---|
| ADR gate: Incorrect/Outdated | ADR 0006 Accepted; identity core implemented; linking/lifecycle still planned | target architecture |
| Domain User: Outdated | removed; Identity owns credentials; no justified Account aggregate | Domain Model, Domain README, debt, security, masterplan |
| Identity absent / Phase 2 not started: Outdated | roleless persistence committed; registration in progress; real authentication incomplete | root/docs/architecture READMEs, context map, masterplan, next steps, security, product scope |
| Production health-only: Incorrect for live tree | registration anonymous in every environment; Household Testing-only | target, next steps, security, summaries |
| Membership 'active' uniqueness: Ambiguous | unfiltered uniqueness on all non-null links; no active state exists | Domain Model, target |
| Verified linking described as a current use case: Incorrect | required target workflow, absent from source | Domain Model |
| Application depends 'only' on Domain: Ambiguous | only project reference is Domain; explicit DI abstractions package also exists | Application README, target |
| Green historical checks: Ambiguous | dated Phase 1 evidence; current 56-test audit and limits are separate | current summaries, next steps, masterplan |
| Error contract: Partially implemented | Household validation/generic 500 use ProblemDetails; registration descriptions/codes/metadata incomplete | target, next steps, debt |
| Feature contexts/event-delivery trigger: Planned/Ambiguous | candidate modules; separate promotion, event, outbox, broker and distribution triggers | context map, target, Domain Model |

Thirteen existing documents were edited, plus this single audit report:

- [Root README](../../README.md): current status and exact dependency graph.
- [Documentation index](../README.md): current snapshot, audit discovery, candidate-context wording.
- [Architecture overview](README.md): honest DDD/current status and readiness exception.
- [Target architecture](target/TARGET-ARCHITECTURE.md): accepted identity, current DI/API/persistence facts, focused transaction guidance, and separate evolution triggers.
- [Domain model](target/DOMAIN-MODEL.md): remove User, classify patterns, distinguish present constraints from future lifecycle.
- [Context map](target/CONTEXT-MAP.md): candidate rather than automatic contexts and explicit promotion criteria.
- [Next steps](roadmap/NEXT-STEPS.md): finish existing registration, strengthen boundaries, then real authentication; retain completed Phase 1 acceptance reference.
- [Masterplan](roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md): Phase 2 is underway; remove retired-User work and avoid duplicate linking implementation across phases.
- [Security roadmap](roadmap/SECURITY-ROADMAP.md): correct registration exposure, Identity state, health metadata and open release gates.
- [Debt register](roadmap/TECHNICAL-DEBT-REGISTER.md): close obsolete TD-006; record TD-016/017/018 for registration/contracts/dependency guards.
- [Product scope](../product/PRODUCT-SCOPE.md): current backend state and accepted Account/Membership decision.
- [Domain README](../../backend/src/HomePlatform.Domain/README.md): actual types and absence of Account aggregate/VO/events.
- [Application README](../../backend/src/HomePlatform.Application/README.md): registration use case, ports and DI exception.

Existing ADRs, historical research/archive snapshots, documentation cleanup map,
and the already-correct migration README were retained. This report is an audit
snapshot; target/roadmap files remain the ongoing decision/sequence authorities.

## Recommended roadmap

### Now

1. Finish registration input validation, normalized uniqueness/race outcomes,
   Application-owned errors and generated HTTP contracts. Preserve the present
   clean port/adapter boundary and current concurrent work.
2. Extend the two existing dependency guards to match the documented forbidden
   frameworks/project directions. Keep this small and explicit.
3. Use this reconciled current-state baseline; do not describe a passing suite
   as proof of missing registration or production-authentication behavior.

### Next

Choose one supported authentication mode and prove real login/challenge/lifecycle
without the fake scheme. Decide Account-reference integrity and deletion/linking
semantics before real Household writes. Then implement Membership resource
authorization and the first authorized mutation through Household with the
needed PostgreSQL concurrency proof. Keep product API DTOs separate and error
mapping consistent.

### Later

Add one justified coordination use case at a time: Tasks, then bounded
recurrence when required, Shopping and authorized Today projections, with
internal Events optional. Define a focused one-transaction invitation acceptance
flow when that lifecycle exists. Exercise migration upgrades, operational
recovery and release controls when the deployment gate is reached.

### Only if triggered

Promote modules to bounded contexts when language/model/lifecycle/ownership
diverge. Add domain events for independent reactions, integration events for
asynchronous consumers, an outbox for atomic reliable publication, and a broker
only when process/throughput/operations justify it. Retain one monolith and
database until independent deployment/scaling/ownership pressure is evidenced.
Generic repositories, UnitOfWork wrappers, event sourcing, sagas and separate
CQRS stores are not planned without a substantially stronger concrete problem.

## Final verification

| Check | Result | Scope / limitation |
|---|---|---|
| Requested solution build, --no-restore --disable-build-servers -m:1 | PASS; 0 warnings, 0 errors | actual live working tree after documentation edits |
| Requested solution tests, --no-build --disable-build-servers -m:1 | PASS; 56/56, 0 skipped | Domain 25, Application 8, Integration 23; TRX results collected outside repo |
| Architecture/dependency tests | PASS; 2/2 included above | exact named-assembly checks only; F04 remains open |
| EF pending-model check with pinned tool, --no-build | PASS; no changes since last migration | model/snapshot comparison; no database modification |
| Fresh migrations | PASS via endpoint test fixtures and isolated probe | PostgreSQL 18.6; existing-data upgrade not verified |
| Additional registration probes | Completed; F01–F03 reproduced | separate disposable database; unchanged assemblies; not committed regression tests |
| Markdown and relative links | PASS | markdownlint on 28 active documents; 135 relative file links in changed documents resolve |
| git diff --check | PASS | final working-tree whitespace check; new report also covered by markdownlint |
| Non-doc files changed by audit | None | content hashes and tracked/non-ignored path set compared with audit-start baseline; only 14 Markdown paths differ |
| Hosted CI, public deployment, current vulnerability/format scan | NOT VERIFIED in this audit | historical Phase 1 results do not establish current status |
| Production/test/configuration/migration code changes | None | documentation-only scope; no commit/push |
