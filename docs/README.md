# Palantir Enterprise .NET 10 Reference Documentation

Welcome to the documentation hub for **Palantir** — a cloud-native .NET 10 + .NET Aspire enterprise microservices reference architecture.

## Quick Start

```bash
dotnet run --project src/Aspire/Palantir.AppHost
```

Open the Aspire Dashboard URL printed in the terminal to inspect all microservices, PostgreSQL, RabbitMQ, Keycloak, microfrontends, OpenTelemetry traces, logs, and metrics.

## Documentation Map

| Section | Description | Link |
| --- | --- | --- |
| **Architecture Reference** | C4 diagrams, Building Blocks catalog, patterns matrix | [docs/architecture/README.md](architecture/README.md) |
| **Architecture Decision Records** | Key architectural choices (VSA, Wolverine, Marten, Orleans, Keycloak) | [docs/architecture/adr](architecture/adr/0001-vertical-slice-architecture.md) |
| **Getting Started** | Prerequisites, local Aspire inner loop, Keycloak login, first API call | [docs/guides/getting-started.md](guides/getting-started.md) |
| **Developer Guide** | Complete walkthrough: Vertical Slices, Building Blocks, Security, Messaging | [docs/guides/developer-guide.md](guides/developer-guide.md) |
| **Testing Guide** | Unit, Architecture, Aspire Integration, and Reqnroll BDD testing | [docs/guides/testing.md](guides/testing.md) |
| **Deployment Guide** | Cloud-native deployment, Kubernetes, OTLP observability, config | [docs/guides/deployment.md](guides/deployment.md) |
| **Accessibility** | WCAG 2.1 AA compliance statement and verification | [docs/ACCESSIBILITY.md](ACCESSIBILITY.md) |
| **AsyncAPI Contracts** | Event-driven message specifications | [docs/asyncapi](asyncapi/order-events-asyncapi.yaml) |
| **EventCatalog** | Domain, event, and service catalog configuration | [docs/eventcatalog](eventcatalog/eventcatalog.config.js) |
