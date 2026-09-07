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
The slice is committed in `e2fca98`, with a verified 72/72-test solution baseline.

Application's only project reference is Domain. Its DI registration helper
uses Microsoft.Extensions.DependencyInjection.Abstractions and registers only
the scoped validator and two handlers. There is no HTTP, EF Core, or Identity dependency.
Trusted actor identity enters through the Application-owned current-account
port, never through a client-owned command
field. See the
[current next steps](../../../docs/architecture/roadmap/NEXT-STEPS.md).
