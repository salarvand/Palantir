# Testing Guide

Palantir uses a **four-layer testing strategy** balancing fast local feedback with full Aspire distributed verification.

## Test Projects

| Project | Framework | Scope |
| --- | --- | --- |
| `tests/Palantir.ArchTests` | `ArchUnitNET` + `xUnit` | Enforces dependency boundaries, sealed Vertical Slice endpoints/validators, DDD invariants. |
| `tests/Palantir.Platform.UnitTests` | `xUnit` + `Shouldly` | Tests Building Blocks (`ValidationSpecification`, `PermissionEvaluator`, `ChannelAuditEmitter`). |
| `tests/Services/*.UnitTests` | `xUnit` + `Shouldly` + `Bogus` | Fast isolated tests for domain aggregates, specifications, validators, and handlers. |
| `tests/Palantir.IntegrationTests` | `Aspire.Hosting.Testing` + `Respawn` + `xUnit` | Boots the real `Palantir.AppHost` topology via `DistributedApplicationTestingBuilder` and tests live HTTP/messaging flows. |
| `tests/Palantir.BddTests` | `Reqnroll.xUnit` + `Aspire.Hosting.Testing` | Executable Gherkin `.feature` specifications running against the live Aspire AppHost. |

## Aspire Integration Testing Pattern

1. `AspireAppFixture` implements `IAsyncLifetime` and is shared via `[CollectionDefinition("Aspire")]` so containers boot once per collection.
2. Startup uses explicit `CancellationTokenSource` timeouts and waits on `ResourceNotifications.WaitForResourceHealthyAsync(...)` before creating clients.
3. Endpoints are always resolved dynamically via `_app.CreateHttpClient(AspireResourceNames.<Service>)` — never hardcoded ports.
4. Relational state resets between mutating tests use `Respawn` (`DbAdapter.Postgres`) via `fixture.ResetDatabaseAsync(...)`.

## Writing BDD Scenarios (Reqnroll)

Add `.feature` files under `tests/Palantir.BddTests/Features/` and `[Binding]` step definitions under `StepDefinitions/` using `BddAspireHooks` clients.
