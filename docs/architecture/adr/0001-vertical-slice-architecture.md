# ADR 0001: Vertical Slice Architecture

## Status
Accepted

## Context
Traditional N-tier layering scatters a single business use case across separate Controllers, Application Services, DTOs, and Repository interfaces, creating high coupling between unrelated features.

## Decision
Every microservice organizes endpoints, commands/queries, validators, specifications, and handlers inside `Features/<Area>/<Action>/` slices implementing `IEndpoint`.

## Consequences
- Features can be added, modified, or removed independently.
- `Palantir.ArchTests` enforces that all service `IEndpoint` and `IValidator<T>` types are `sealed` and reside under `Features/`.
