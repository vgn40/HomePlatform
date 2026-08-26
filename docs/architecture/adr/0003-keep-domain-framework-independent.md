# Keep Domain framework-independent

Status: Accepted

## Context

Business rules should not be coupled to delivery or persistence technology.

## Decision

Domain references no ASP.NET Core, EF Core, PostgreSQL, logging framework, or external SDK.

## Consequences

Technical adapters remain outside Domain. Mapping at boundaries is explicit.
