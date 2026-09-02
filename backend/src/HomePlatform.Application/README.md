# HomePlatform.Application

Application contains explicit use cases, orchestration, ports, and
boundary-neutral results. `CreateHousehold`, `IHouseholdRepository`, and
`ICurrentAccount` implement the first Name-only use case. Focused tests prove
trusted actor ownership, validation zero-write behavior, cancellation
forwarding, and failure propagation; the Testing-only HTTP/PostgreSQL path is
also proven.

Application depends only on Domain. Trusted actor identity enters through the
Application-owned current-account port, never through a client-owned command
field. Stable expected validation/unauthenticated outcomes remain follow-up
work. See the [current next steps](../../../docs/architecture/roadmap/NEXT-STEPS.md).
