# Use PostgreSQL

Status: Accepted

## Context

The application needs a reliable relational database with strong transactional behavior.

## Decision

Use PostgreSQL 18 for local development and production persistence.

## Consequences

Integration tests target real PostgreSQL behavior rather than SQLite or an in-memory substitute.
