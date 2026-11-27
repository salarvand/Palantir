# Getting Started

## Prerequisites
- **.NET 10 SDK** (matching `global.json`)
- **Docker Desktop** or **Podman** (required by Aspire for PostgreSQL, RabbitMQ, and Keycloak containers)
- **Node.js 20+** (for frontend MFEs under `src/Clients/`)

## 1. Install Frontend Dependencies (Optional for Backend-Only Work)
```bash
cd src/Clients
npm install
```

## 2. Run the Full Distributed System via .NET Aspire
From the repository root:
```bash
dotnet run --project src/Aspire/Palantir.AppHost
```

Aspire will automatically:
1. Pull and start **PostgreSQL 16 + pgvector**, **RabbitMQ** (with management plugin), and **Keycloak 26** (importing `deploy/keycloak/palantir-realm.json`).
2. Start all 9 domain microservices, the YARP Gateway, and frontend dev servers.
3. Launch the **Aspire Dashboard** with live OpenTelemetry traces, metrics, structured logs, and service endpoints.

## 3. Verify Service Health & OpenAPI Docs
- Navigate to any service's `/health` or `/alive` endpoint from the Aspire Dashboard.
- Open `/scalar/v1` on any service (in Development) to browse interactive OpenAPI documentation.
- Open `/asyncapi` on messaging services to inspect Saunter AsyncAPI specs.
