# Deployment & Operations Guide

## Cloud-Native Readiness

Every Palantir service exposes:
- **`/health`**: Full readiness check including `PostgresHealthCheck` (`SELECT 1;`).
- **`/alive`**: Liveness probe (`live` tag).
- **Graceful Shutdown**: Configured with a 15-second `HostOptions.ShutdownTimeout` allowing `AuditPipelineService`, `Wolverine`, and `Quartz` hosted services to drain pending work.
- **OpenTelemetry (OTLP)**: Configured via `OTEL_EXPORTER_OTLP_ENDPOINT` for traces, metrics, and audit activities (`Palantir.Auditing`).

## Required Environment Configuration

| Key | Purpose |
| --- | --- |
| `ConnectionStrings__PalantirDb` | PostgreSQL 16 connection string (injected automatically by Aspire). |
| `ConnectionStrings__rabbitmq` | RabbitMQ AMQP connection string (injected automatically by Aspire). |
| `Keycloak__Url` | Base URL of the Keycloak realm server (`http://localhost:8080` in dev). |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OpenTelemetry collector endpoint. |

## CI/CD Workflows
- `.github/workflows/backend-ci.yml`: Restores, builds, and executes unit, architecture, and Aspire integration/BDD suites on Linux runners (`ubuntu-latest` with Docker).
- `.github/workflows/frontend-ci.yml`: Lints, unit-tests (`Vitest`), and runs Playwright accessibility checks.
