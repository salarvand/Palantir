# ADR 0003: Marten Event Sourcing on PostgreSQL

## Status
Accepted

## Context
Order lifecycle state transitions require complete immutable history for regulatory compliance and temporal replay while minimizing operational infrastructure.

## Decision
Use Marten (`Palantir.Platform.EventStore`) on the consolidated PostgreSQL 16 instance for event streams and async read projections (`OrderBlotterProjection`).

## Consequences
- Single relational+document+event-store engine (PostgreSQL) simplifies Aspire orchestration and transactional outbox integration.
