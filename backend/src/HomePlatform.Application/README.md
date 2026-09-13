# HomePlatform.Application

Application contains explicit use cases, orchestration, ports, and
boundary-neutral results. `CreateHousehold`, `IHouseholdRepository`, and
`ICurrentAccount` implement the first Name-only use case. Focused tests prove
explicit Success/Invalid/Unauthenticated outcomes, trusted actor ownership,
zero writes for invalid and unauthenticated inputs, cancellation forwarding,
and unexpected failure propagation; the Testing-only HTTP/PostgreSQL path is
also proven.

The implemented Account Registration slice includes RegisterAccountCommand/Handler,
RegisterAccountValidator, and
the IAccountRegistration port with an Application-owned AccountRegistrationResult.
The handler validates input and delegates to the port. Infrastructure maps
UserManager errors and the recognized PostgreSQL duplicate race to typed,
Application-owned error codes; framework descriptions do not cross the port.
The registration slice is committed in `e2fca98`. Bearer sign-in is implemented
in `4180096` through SignInAccountCommand/Handler, SignInAccountValidator, and
the IAccountAuthentication port. Refresh is implemented through
RefreshAccountCommand/Handler, RefreshAccountValidator, and IAccountRefresh.
The handler rejects missing/blank tokens with RefreshTokenRequired and forwards
nonblank tokens and cancellation unchanged. IdentityAccountRefresh in
Infrastructure validates expiry and the security stamp, returns the typed
InvalidRefreshToken failure, or delegates new access/refresh-token issuance to
the framework bearer handler. API maps anonymous POST /api/accounts/refresh
and returns Results.Empty after the framework writes AccessTokenResponse.

Permanent Application tests use a recording fake; real-bearer PostgreSQL tests
prove registration -> sign-in -> refresh -> protected Household request ->
persisted AccountId, safe errors, deterministic expiry, changed security stamp,
and anonymous refresh under an authenticated fallback policy.

Assemblies represent architectural layers; top-level Application/API folders
represent business areas; use-case folders represent concrete operations.
Account slices use `Accounts/<UseCase>/`: `Register/`, `SignIn/`, and `Refresh/`.
Each slice keeps its operation-specific ports, results, and errors locally.
Identity adapters remain in Infrastructure/Identity.

Application's only project reference is Domain. Its DI registration helper
uses Microsoft.Extensions.DependencyInjection.Abstractions and registers only
three scoped validators and four scoped handlers. There is no HTTP, EF Core, or Identity dependency.
Trusted actor identity enters through the Application-owned current-account
port, never through a client-owned command
field. See the
[current next steps](../../../docs/architecture/roadmap/NEXT-STEPS.md).
