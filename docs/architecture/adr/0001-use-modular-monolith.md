# Use a modular monolith

Status: Accepted

## Context

The product is new and needs clear boundaries without distributed-system overhead.

## Decision

Build one deployable application with explicit internal modules and layered dependencies.

## Consequences

Deployment and local development stay simple. Module boundaries must be enforced in code and review.
