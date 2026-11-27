# Comprehensive Developer Guide

This guide explains how Palantir is structured, how its Building Blocks work, and how to build features that adhere to the platform's architectural invariants.

---

## 1. Repository Layout & Dependency Rules

```text
Palantir/
├── src/
│   ├── Aspire/
│   │   ├── Palantir.AppHost/          # Aspire orchestration topology
│   │   └── Palantir.ServiceDefaults/  # OpenTelemetry, service discovery, OpenAPI/AsyncAPI, /health
│   ├── BuildingBlocks/
│   │   ├── Palantir.Constants/        # Zero-dependency resource, queue, and permission constants
│   │   ├── Palantir.SharedKernel/     # Pure DDD domain primitives & specifications
│   │   ├── Palantir.Platform/         # Service chassis & cross-cutting infrastructure blocks
│   │   ├── Palantir.Generators*/      # Roslyn source generators
│   ├── Gateway/Palantir.Gateway/      # YARP reverse proxy
│   ├── Services/                      # 9 autonomous Vertical Slice microservices
│   └── Clients/                       # Module Federation MFEs + Next.js Multi-Zones
└── tests/
    ├── Palantir.ArchTests/            # ArchUnitNET architecture invariants
    ├── Palantir.IntegrationTests/     # Aspire DistributedApplicationTestingBuilder tests
    ├── Palantir.BddTests/             # Reqnroll Gherkin + Aspire acceptance tests
    ├── Palantir.Platform.UnitTests/   # Unit tests for SharedKernel & Platform blocks
    └── Services/*.UnitTests/          # Per-service domain & validator unit tests
```

### Enforced Dependency Invariants (`Palantir.ArchTests`)
1. `Palantir.Constants` has **zero** dependencies on `Palantir.SharedKernel` or `Palantir.Platform`.
2. `Palantir.SharedKernel` has **zero** dependencies on `Palantir.Platform`, EF Core, Marten, Npgsql, or Wolverine.
3. Microservices **never** reference each other directly (communicate only via HTTP/gRPC/RabbitMQ).
4. All service `IEndpoint` and `IValidator<T>` classes must be `sealed` and reside under `Features/`.

---

## 2. Building Blocks Deep Dive

### A. Unified Specification Pattern (Domain + HTTP Validation)
Defined in `Palantir.SharedKernel/Specifications.cs` and bridged to ASP.NET Core in `Palantir.Platform/Validation/`:

1. **Define a Specification once**:
   ```csharp
   public sealed class OrderQuantitySpecification : ValidationSpecification<CreateOrderCommand>
   {
       public override string RuleName => nameof(CreateOrderCommand.Quantity);
       public override string ErrorMessage => "Quantity must be greater than zero.";
       public override bool IsSatisfiedBy(CreateOrderCommand cmd) => cmd.Quantity > 0;
   }
   ```
2. **Compose rules** with `.And()`, `.Or()`, `.Not()`.
3. **Reuse at the HTTP edge** via `SpecificationValidator<T>` (FluentValidation) or `.WithSpecificationValidation(spec1, spec2)`:
   ```csharp
   public sealed class CreateOrderValidator : SpecificationValidator<CreateOrderCommand>
   {
       public CreateOrderValidator()
       {
           UseSpecification(new OrderQuantitySpecification());
       }
   }
   ```
4. **Reuse inside Domain / Command Handlers**:
   ```csharp
   command.EnsureSatisfies(new OrderQuantitySpecification()); // Throws DomainSpecificationException -> HTTP 400 ProblemDetails
   ```

### B. Non-Blocking Multi-Sink Auditing
`AddPalantirAuditing` (automatically wired by `AddPalantirChassis`) registers:
- `IAuditEmitter` (`ChannelAuditEmitter`) backed by a bounded `Channel<AuditEntry>` (`DropOldest`, capacity 10,000) — **O(1) non-blocking** on request threads.
- `AuditPipelineService` (`BackgroundService`) draining batches up to 100 entries to all registered `IAuditSink` adapters in parallel:
  - `PostgresAuditSink`: high-throughput `COPY` binary import into `platform_audit.audit_entries`.
  - `OtlpAuditSink`: OpenTelemetry `ActivitySource("Palantir.Auditing")` spans/tags.
  - `StructuredLogAuditSink`: structured `ILogger` audit records.
  - `RabbitMqAuditSink` (opt-in): publishes `AuditEntry` via isolated DI scopes to Wolverine/RabbitMQ.

Usage in any handler:
```csharp
auditEmitter.Emit(AuditEntry.Create(
    "OrderSubmitted", "OmsService", "Order", orderId.ToString(), "Create",
    new Dictionary<string, object?> { ["Symbol"] = symbol }));
```

### C. Dimension-Resource-Context Permission Management
`Palantir.Platform.Security.Permissions` implements specification-driven ABAC/PBAC authorization on top of Keycloak tokens:
- **Dimension** (`PermissionDimension`): functional domain (`Orders`, `Risk`, `Portfolio`, `MarketData`, `Settlement`, `Audit`, `Iam`, `System`).
- **Resource** (`ResourceTemplate`): target entity (`Type`, optional `Id`, optional `OwnerId`).
- **Context** (`PermissionContext`): runtime conditions (`SubjectUserId`, `ClientIpAddress`, `MarketState`, `IsVpnConnected`, `TenantId`, `CustomAttributes`).
- **Evaluator** (`IPermissionEvaluator`): evaluates grants against `IPermissionSpecification` rules (`DimensionActionSpec`, `ResourceMatchSpec`, `OwnershipSpec`, `TradingHoursSpec`, `NetworkConditionSpec`).

Endpoints can require either flat permission constants (`RequireAuthorization(Permissions.AllowPlaceOrder)`) or explicit dimensions (`RequireDimension(PermissionDimension.Orders, "Place", "Order")`) — both flow through `IPermissionEvaluator`.

### D. Additional Enterprise Chassis Blocks
Wired automatically by `AddPalantirChassis` / `UsePalantirChassis` (or opt-in):
- **Health Checks & Graceful Shutdown** (`HealthChecks/HealthCheckExtensions.cs`): PostgreSQL `SELECT 1` readiness probe + 15s shutdown grace period.
- **Rate Limiting** (`RateLimiting/RateLimitingExtensions.cs`): Global partitioned fixed-window limiter returning HTTP `429 Too Many Requests`.
- **Background Scheduling** (`Scheduling/QuartzExtensions.cs`): Opt-in `services.AddPalantirScheduling(connectionString)` using Quartz.NET with PostgreSQL persistent store and `System.Text.Json`.
- **Messaging Dead-Letter Handling** (`Messaging/DeadLetterExtensions.cs`): Transient retry cooldowns (`TimeoutException`, `NpgsqlException`) followed by Wolverine error-queue routing.

---

## 3. Adding a New Vertical Slice Feature

1. Create `src/Services/<Domain>/Palantir.<Service>/Features/<Area>/<Action>/<Action>Feature.cs`.
2. Define the request/command record, `sealed` `ValidationSpecification<T>` rules, `sealed` `SpecificationValidator<T>`, handler, and `internal sealed` `IEndpoint`.
3. Emit audit events via `IAuditEmitter` and enforce authorization via `.RequireAuthorization(...)` or `.RequireDimension(...)`.
4. Add unit tests in `tests/Services/Palantir.<Service>.UnitTests/` and BDD/Integration scenarios if cross-service.

---

## 4. Adding a New Microservice

1. Create `src/Services/<Name>/Palantir.<Name>Service/` referencing `Palantir.ServiceDefaults`, `Palantir.Platform`, `Palantir.SharedKernel`, `Palantir.Constants`.
2. In `Program.cs`, call `builder.AddServiceDefaults()`, `builder.AddPalantirChassis(typeof(Program).Assembly)`, `app.MapDefaultEndpoints()`, and `app.UsePalantirChassis()`.
3. Register the resource name in `Palantir.Constants.AspireResourceNames` and wire it in `src/Aspire/Palantir.AppHost/Program.cs` and `Palantir.Gateway/appsettings.json`.
4. Add `<Service>.UnitTests` under `tests/Services/` and register the assembly in `Palantir.ArchTests/ArchitectureTests.cs`.
