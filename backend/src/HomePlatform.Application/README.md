# HomePlatform.Application

Application contains explicit use cases, orchestration, ports, and
boundary-neutral results. The current working tree contains an incomplete
`CreateHousehold` use case and `IHouseholdRepository`; it is not yet exposed or
proven end to end.

Application must depend only on Domain. Trusted actor identity will enter
through an Application-owned current-account port, never through a client-owned
command field. See the [current next steps](../../../docs/architecture/roadmap/NEXT-STEPS.md).
