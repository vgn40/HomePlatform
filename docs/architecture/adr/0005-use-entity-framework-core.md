# Use Entity Framework Core

Status: Accepted

## Context

The backend needs a maintainable PostgreSQL persistence adapter for .NET.

## Decision

Use Entity Framework Core with the Npgsql provider inside Infrastructure.

## Consequences

Domain remains persistence-ignorant. Migrations and mappings belong to Infrastructure.
