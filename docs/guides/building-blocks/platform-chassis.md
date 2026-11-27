# `Palantir.Platform` — Enterprise Service Chassis

`Palantir.Platform` ([ServiceChassisExtensions.cs](../../../src/BuildingBlocks/Palantir.Platform/ServiceChassisExtensions.cs)) encapsulates all cross-cutting microservice concerns behind `builder.AddPalantirChassis(assembly)` and `app.UsePalantirChassis()`.

---

## 1. Chassis Registration Pipeline

| Call Order | Setup Method | Middleware / Runtime Behavior |
| --- | --- | --- |
| 1 | `AddPalantirCaching(connectionString)` | Registers `PostgresKeyValueStore` (`IKeyValueStore` + `IDistributedCache`) + `FusionCache` L1/L2 with fail-safe. |
| 2 | `AddPalantirSecurity(keycloakUrl)` | Registers RFC 8693 `ITokenExchangeService`, `IPermissionTokenManager`, `IPermissionEvaluator`, `IPermissionSpecification`s, and authorization handlers. |
| 3 | `AddPalantirJwtAuthentication(...)` | Configures `JwtBearerDefaults` against Keycloak realm `/realms/palantir`. |
| 4 | `AddPalantirProblemDetails()` | Adds `DomainSpecificationExceptionHandler` + RFC 7807 `ProblemDetails` enriched with `traceId` and `timestamp`. |
| 5 | `AddPalantirApiVersioning()` | Configures URL segment (`/api/v1/...`) and `api-version` header versioning. |
| 6 | `AddPalantirFeatureFlags()` | Enables `Microsoft.FeatureManagement` + `.RequireFeature(...)` filter. |
| 7 | `AddPalantirValidators(assembly)` | Scans and registers FluentValidation validators (including `SpecificationValidator<T>`). |
| 8 | `AddEndpoints(assembly)` | Scrutor-scans all `IEndpoint` classes in the service assembly. |
| 9 | `AddPalantirAuditing(connectionString)` | Registers bounded `Channel<AuditEntry>`, `IAuditEmitter`, sinks, and `AuditPipelineService`. |
| 10 | `AddPalantirHealthChecks(connectionString)` | Registers `PostgresHealthCheck` (`SELECT 1;`) and 15-second `HostOptions.ShutdownTimeout`. |
| 11 | `AddPalantirRateLimiting()` | Configures partitioned fixed-window global rate limiting (`429 Too Many Requests`). |

Runtime middleware order in `UsePalantirChassis()`:
1. `UsePalantirExceptionHandler()`
2. `UseAuthentication()`
3. `UseAuthorization()`
4. `UsePalantirRateLimiting()`
5. `MapEndpoints(app.NewV1ApiVersionSet(), resourceName)`

---

## 2. Subsystem Walkthrough

### Endpoints, Versioning & Idempotency
- [EndpointExtensions.cs](../../../src/BuildingBlocks/Palantir.Platform/Endpoints/EndpointExtensions.cs): Defines `IEndpoint` arities (`IEndpoint`, `IEndpoint<TResult>`, up to 3 request/service parameters), `ProducesPost<T>()`, `ProducesGet<T>()`, `ProducesPut()`, `ProducesDelete()`, and `WithIdempotency()` (echoes `x-request-id`).
- [ApiVersioningExtensions.cs](../../../src/BuildingBlocks/Palantir.Platform/Endpoints/ApiVersioningExtensions.cs): Defines `ApiVersioningExtensions.V1` and `NewV1ApiVersionSet()`.

### Unified Validation & Exception Mapping
- [SpecificationValidatorAdapter.cs](../../../src/BuildingBlocks/Palantir.Platform/Validation/SpecificationValidatorAdapter.cs): `SpecificationValidator<T>` lets FluentValidation validators invoke `UseSpecification(spec)` or `UseSpecification(selector, spec)`.
- [FluentValidationExtensions.cs](../../../src/BuildingBlocks/Palantir.Platform/Validation/FluentValidationExtensions.cs): Provides `.WithValidation<TRequest>()` (FluentValidation filter) and `.WithSpecificationValidation<TRequest>(specs...)` (direct `IValidationSpecification<TRequest>` endpoint filter).
- [ProblemDetailsExtensions.cs](../../../src/BuildingBlocks/Palantir.Platform/Exceptions/ProblemDetailsExtensions.cs): `DomainSpecificationExceptionHandler` converts `DomainSpecificationException` thrown inside handlers/domain into HTTP 400 `ValidationProblemDetails`.

### Non-Blocking Multi-Sink Auditing
Located under `src/BuildingBlocks/Palantir.Platform/Auditing/`:
- [AuditEntry.cs](../../../src/BuildingBlocks/Palantir.Platform/Auditing/AuditEntry.cs): Immutable record (`AuditId`, `EventType`, `ServiceName`, `UserId`, `CorrelationId`, `ResourceType`, `ResourceId`, `Action`, `Payload`, `TimestampUtc`).
- [ChannelAuditEmitter.cs](../../../src/BuildingBlocks/Palantir.Platform/Auditing/ChannelAuditEmitter.cs): Non-blocking `channel.Writer.TryWrite(entry)` over a bounded channel (`DropOldest`, capacity `10_000`).
- [AuditPipelineService.cs](../../../src/BuildingBlocks/Palantir.Platform/Auditing/AuditPipelineService.cs): `BackgroundService` batching up to 100 entries and flushing all sinks concurrently via `Task.WhenAll`, draining remaining entries during graceful shutdown (`StopAsync`).
- **Sinks**:
  - [PostgresAuditSink.cs](../../../src/BuildingBlocks/Palantir.Platform/Auditing/Sinks/PostgresAuditSink.cs): Auto-creates `platform_audit.audit_entries` and writes batches with PostgreSQL `COPY ... FROM STDIN (FORMAT BINARY)`.
  - [OtlpAuditSink.cs](../../../src/BuildingBlocks/Palantir.Platform/Auditing/Sinks/OtlpAuditSink.cs): Emits spans on `ActivitySource("Palantir.Auditing")`, automatically collected by `ServiceDefaults`.
  - [StructuredLogAuditSink.cs](../../../src/BuildingBlocks/Palantir.Platform/Auditing/Sinks/StructuredLogAuditSink.cs): Writes structured `ILogger` audit records.
  - [RabbitMqAuditSink.cs](../../../src/BuildingBlocks/Palantir.Platform/Auditing/Sinks/RabbitMqAuditSink.cs): Opt-in (`EnableRabbitMqSink = true`), resolves scoped `IMessageBus` via `IServiceScopeFactory`.

### Dimension-Resource-Context Security & Permissions
Located under `src/BuildingBlocks/Palantir.Platform/Security/`:
- [Security.cs](../../../src/BuildingBlocks/Palantir.Platform/Security/Security.cs):
  - `KeycloakTokenExchangeService`: Exchanges incoming Bearer tokens for target audience tokens via OAuth 2.0 RFC 8693 (`urn:ietf:params:oauth:grant-type:token-exchange`) and caches them in `IKeyValueStore`.
  - `TokenExchangeDelegatingHandler`: Transparently swaps downstream `HttpClient` Authorization headers.
  - `PostgresPermissionTokenManager`: Caches serialized `PermissionToken` payloads in `platform_cache`.
- `Permissions/` subfolder:
  - [PermissionDimension.cs](../../../src/BuildingBlocks/Palantir.Platform/Security/Permissions/PermissionDimension.cs): Functional domains (`Orders`, `MarketData`, `Risk`, `Portfolio`, `Settlement`, `Audit`, `Iam`, `System`).
  - [ResourceTemplate.cs](../../../src/BuildingBlocks/Palantir.Platform/Security/Permissions/ResourceTemplate.cs): Target resource (`Type`, optional `Id`, optional `OwnerId`).
  - [PermissionContext.cs](../../../src/BuildingBlocks/Palantir.Platform/Security/Permissions/PermissionContext.cs): Runtime HTTP/environment attributes (`SubjectUserId`, `ClientIpAddress`, `MarketState` from `X-Market-State`, `IsVpnConnected` from `X-VPN-Connected`, `TenantId` from `X-Tenant-Id`).
  - [PermissionSpecifications.cs](../../../src/BuildingBlocks/Palantir.Platform/Security/Permissions/PermissionSpecifications.cs): `DimensionActionSpec`, `ResourceMatchSpec`, `OwnershipSpec`, `TradingHoursSpec`, `NetworkConditionSpec`.
  - [PermissionEvaluator.cs](../../../src/BuildingBlocks/Palantir.Platform/Security/Permissions/PermissionEvaluator.cs): Evaluates grants using `request.Validate(_specifications)` from `SharedKernel`.
  - [DimensionAuthorizationHandler.cs](../../../src/BuildingBlocks/Palantir.Platform/Security/Permissions/DimensionAuthorizationHandler.cs): Enables `.RequireDimension(PermissionDimension.Orders, "Place", "Order")` on endpoints alongside legacy `.RequireAuthorization(Permissions.AllowPlaceOrder)`.

### Messaging, Persistence, EventStore, Caching, Scheduling & gRPC
- **Messaging & Dead-Letter Handling** ([WolverineExtensions.cs](../../../src/BuildingBlocks/Palantir.Platform/Messaging/WolverineExtensions.cs), [DeadLetterExtensions.cs](../../../src/BuildingBlocks/Palantir.Platform/Messaging/DeadLetterExtensions.cs)): Configures RabbitMQ auto-provisioning, PostgreSQL durable outbox/inbox (`wolverine` schema), EF Core transactions, retry cooldowns for `TimeoutException` / `NpgsqlException`, and dead-letter error queue routing.
- **Persistence & Event Store** ([Interceptors.cs](../../../src/BuildingBlocks/Palantir.Platform/Persistence/Interceptors.cs), [PostgresSchemaExtensions.cs](../../../src/BuildingBlocks/Palantir.Platform/Persistence/PostgresSchemaExtensions.cs), [MartenExtensions.cs](../../../src/BuildingBlocks/Palantir.Platform/EventStore/MartenExtensions.cs)): EF Core interceptors (`PublishDomainEventsInterceptor`, `AuditableEntityInterceptor`), retry-safe `EnsurePostgresSchemaCreatedAsync<TContext>`, and Marten `event_store` with async projection daemon + Wolverine integration.
- **Caching** ([CachingExtensions.cs](../../../src/BuildingBlocks/Palantir.Platform/Caching/CachingExtensions.cs)): Unlogged PostgreSQL `platform_cache` table bridged to `FusionCache` (`5 min` default TTL, `1 hr` fail-safe).
- **gRPC** ([GrpcExtensions.cs](../../../src/BuildingBlocks/Palantir.Platform/Grpc/GrpcExtensions.cs)): `AddResilientGrpcClient<TClient>` with 4-attempt exponential backoff on `Unavailable`/`ResourceExhausted` and optional RFC 8693 token exchange.
- **Background Scheduling (Opt-In)** ([QuartzExtensions.cs](../../../src/BuildingBlocks/Palantir.Platform/Scheduling/QuartzExtensions.cs)): `AddPalantirScheduling(connectionString)` configures Quartz.NET with PostgreSQL persistent job store (`UseProperties = true`, `UsePostgres`, `UseSystemTextJsonSerializer`) and graceful job completion on shutdown.
