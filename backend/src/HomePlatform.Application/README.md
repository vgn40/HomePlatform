# HomePlatform.Application

Application contains explicit use cases, orchestration, ports, and
boundary-neutral results. `CreateHousehold`, `IHouseholdRepository`, and
`ICurrentAccount` implement the first Name-only use case. Focused tests prove
explicit Success/Invalid/Unauthenticated outcomes, trusted actor ownership,
zero writes for invalid and unauthenticated inputs, cancellation forwarding,
and unexpected failure propagation; the Testing-only HTTP/PostgreSQL path is
also proven.

Application depends only on Domain. Trusted actor identity enters through the
Application-owned current-account port, never through a client-owned command
field. See the
[current next steps](../../../docs/architecture/roadmap/NEXT-STEPS.md).
