# ADR 0002: WolverineFx Messaging & Durable Outbox

## Status
Accepted

## Context
Microservices require reliable asynchronous messaging over RabbitMQ with transactional outbox guarantees and dead-letter handling without boilerplate mediator glue.

## Decision
Use WolverineFx (`WolverineFx`, `WolverineFx.RabbitMQ`, `WolverineFx.Postgresql`) configured via `AddPalantirMessaging` and `AddPalantirDeadLetterHandling`.

## Consequences
- Outgoing messages share the service's PostgreSQL transaction (EF Core or Marten).
- Transient failures (`TimeoutException`, `NpgsqlException`) retry with cooldown before routing to the dead-letter error queue.
