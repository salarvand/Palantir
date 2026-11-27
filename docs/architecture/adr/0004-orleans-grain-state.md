# ADR 0004: Microsoft Orleans for Order State Concurrency

## Status
Accepted

## Context
Concurrent order submissions, fills, cancellations, and GTT expirations for a single order ID risk race conditions under horizontal scaling.

## Decision
Model each active order as a single-threaded Orleans virtual actor (`OrderGrain`) backed by ADO.NET PostgreSQL clustering, storage, and reminders.

## Consequences
- Turn-based grain execution serializes mutations per `OrderId` without distributed locks.
