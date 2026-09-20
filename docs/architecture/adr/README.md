# Architecture Decision Records

ADRs preserve decisions in chronological order. Accepted ADRs are historical
authority; a later ADR supersedes rather than rewrites them. Proposed ADRs are
review gates and must not be described as implemented or approved.

| ADR | Status | Decision |
|---|---|---|
| [0001](0001-use-modular-monolith.md) | Accepted | Use a modular monolith |
| [0002](0002-use-csharp-dotnet-backend.md) | Accepted | Use C# and .NET for the backend |
| [0003](0003-keep-domain-framework-independent.md) | Accepted | Keep Domain framework-independent |
| [0004](0004-use-postgresql.md) | Accepted | Use PostgreSQL |
| [0005](0005-use-entity-framework-core.md) | Accepted | Use Entity Framework Core |
| [0006](0006-separate-account-and-household-membership-identity.md) | Accepted; Person deferral/direct-link target superseded by 0007 | Separate Account and Household Membership identity; historical text preserved |
| [0007](0007-person-as-stable-human-identity.md) | Accepted TARGET; not implemented | Person as stable human identity separate from Account and HouseholdMembership |

## Status meanings

- **Proposed:** under review; implementation depending on it is blocked.
- **Accepted:** approved and authoritative.
- **Rejected:** considered and not selected.
- **Superseded:** retained for history and replaced by a later ADR.

The next available ADR number is 0008. ADR 0006 was explicitly accepted on
2026-08-30; its acceptance does not claim every linking, lifecycle,
authorization, or concurrency consequence is implemented.
