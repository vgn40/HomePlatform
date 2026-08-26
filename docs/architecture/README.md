# Architecture

## Style

HomePlatform is a modular monolith: one deployable application with explicit internal boundaries.

## Layering

```text
API / Presentation
        ↓
Application
        ↓
Domain
```

Infrastructure contains technical adapters around the inner layers and may depend on Application and Domain. API is the composition root and depends on Application and Infrastructure.

## Core rule

Domain must remain framework-independent. Application must not depend on Infrastructure or API.

## Why a modular monolith

- Simple deployment
- Transactional consistency
- Clear boundaries
- Easy local development
- No distributed-system complexity
- Modules can be separated later only if a concrete need appears

## Future module shape

Illustrative example only:

```text
HomePlatform.Domain/
  Households/

HomePlatform.Application/
  Households/
    CreateHousehold/

HomePlatform.Infrastructure/
  Households/

HomePlatform.Api/
  Endpoints/
    Households/
```

This is illustrative only. No domain modules are created until the domain is modelled.

## Decisions

Short architecture decision records are stored in [`adr/`](adr/).
