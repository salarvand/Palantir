# Feature Implementation Plan: Replace Elasticsearch with Meilisearch

* **Feature ID**: `replace-elasticsearch-with-meilisearch`
* **Status**: `Approved`
* **Approved By**: `System Architect`
* **Approved At**: `2026-11-24T11:01:00Z`
* **Specification Source**: User Request: Replace Elasticsearch with Meilisearch across solution conforming to SOLID and zero-Azure containerized architecture

---

## 1. Executive Summary & Raw Requirements
The objective is to replace **Elasticsearch** (`docker.elastic.co/elasticsearch/elasticsearch:8.17.0`) with **Meilisearch** (`getmeili/meilisearch:v1.12`) across the Palantir trading platform reference architecture (`d:\01\01\07\PalantirCode`).

Requirements (Aligned through interactive design interview):
1. Replace Elasticsearch container in Aspire AppHost with Meilisearch container (port 7700, data volume `meilisearch-data`, analytics disabled, local development master key).
2. Update centralized resource name constants in `Palantir.Constants` from `Elasticsearch` to `Meilisearch`.
3. Update centralized package management in `Directory.Packages.props` by removing `Elastic.Clients.Elasticsearch` and adding `MeiliSearch` (v0.20.0).
4. Refactor `Palantir.AuditService` to follow SOLID design principles:
   - Introduce `IAuditLogRepository` abstraction and `MeilisearchAuditLogRepository` implementation (Single Responsibility & Dependency Inversion).
   - Document ingestion uses high-throughput non-blocking task enqueue.
   - Create `MeilisearchIndexInitializer` hosted service to ensure index creation with `auditId` primary key and proper filterable (`symbol`, `auditId`, `eventType`, `clientId`, `orderId`) and sortable (`timestampUtc`) attributes.
   - Refactor `AuditEndpoints` to depend on `IAuditLogRepository` instead of concrete SDK clients.
5. Update Admin MFE UI heading in `AdminApp.tsx` from Elasticsearch to Meilisearch.
6. Synchronize governance artifacts (`.insight/bootstrap/`, architecture docs).
7. Avoid automatically running `dotnet build` or `dotnet run` commands.

---

## 2. Requirements Matrix

* **[REQ-01]**: In `src/Aspire/Palantir.AppHost/Program.cs`, replace Elasticsearch container with `getmeili/meilisearch:v1.12` and wire URL/ApiKey into `Palantir.AuditService`.
* **[REQ-02]**: In `src/BuildingBlocks/Palantir.Constants/SystemConstants.cs`, define `AspireResourceNames.Meilisearch = "meilisearch"`.
* **[REQ-03]**: In `Directory.Packages.props`, replace `Elastic.Clients.Elasticsearch` (v8.17.0) with `MeiliSearch` (v0.20.0).
* **[REQ-04]**: In `src/Services/Audit/Palantir.AuditService/Palantir.AuditService.csproj`, reference `MeiliSearch`.
* **[REQ-05]**: In `src/Services/Audit/Palantir.AuditService/`, introduce `IAuditLogRepository` and `MeilisearchAuditLogRepository`.
* **[REQ-06]**: In `src/Services/Audit/Palantir.AuditService/`, implement `MeilisearchIndexInitializer` configuring index `audit-records-v1`, primary key, filterable attributes, and sortable attributes.
* **[REQ-07]**: In `src/Services/Audit/Palantir.AuditService/Endpoints/AuditEndpoints.cs`, inject `IAuditLogRepository`.
* **[REQ-08]**: In `src/Clients/admin-mfe/src/AdminApp.tsx`, update UI labels to reflect Meilisearch.
* **[REQ-09]**: Update solution documentation and `.insight` repository map.

---

## 3. In-Scope Modules & Target Files

### Aspire & Constants
* `src/BuildingBlocks/Palantir.Constants/SystemConstants.cs` (Modify)
* `src/Aspire/Palantir.AppHost/Program.cs` (Modify)
* `Directory.Packages.props` (Modify)

### Audit Service
* `src/Services/Audit/Palantir.AuditService/Palantir.AuditService.csproj` (Modify)
* `src/Services/Audit/Palantir.AuditService/Domain/RegulatoryAuditRecord.cs` (Modify)
* `src/Services/Audit/Palantir.AuditService/Infrastructure/IAuditLogRepository.cs` (Create)
* `src/Services/Audit/Palantir.AuditService/Infrastructure/MeilisearchAuditLogRepository.cs` (Create)
* `src/Services/Audit/Palantir.AuditService/Infrastructure/MeilisearchIndexInitializer.cs` (Create)
* `src/Services/Audit/Palantir.AuditService/Endpoints/AuditEndpoints.cs` (Modify)
* `src/Services/Audit/Palantir.AuditService/Program.cs` (Modify)

### Microfrontend
* `src/Clients/admin-mfe/src/AdminApp.tsx` (Modify)

### Documentation & Bootstrap
* `.insight/bootstrap/README.md` (Modify)
* `.insight/bootstrap/repository-map.json` (Modify)
* `docs/palantir_implementation_plan.md` (Modify)
* `docs/palantir_multi_phase_implementation_guide.md` (Modify)
* `docs/palantir_plan.md` (Modify)

---

## 4. Verification Plan
* Codebase audit ensuring zero remaining references to `Elastic.Clients.Elasticsearch`.
* Verification of `MeilisearchClient` options, endpoint wiring, and index settings.
* Manual inspection of minimal API contracts.

---

## 5. Executable Agent Prompt Block
```markdown
Execute the replacement of Elasticsearch with Meilisearch according to feature plan `replace-elasticsearch-with-meilisearch`.
1. Update `Directory.Packages.props` to replace `Elastic.Clients.Elasticsearch` with `MeiliSearch` (0.20.0).
2. Update `Palantir.Constants` to define `AspireResourceNames.Meilisearch`.
3. Update `Palantir.AppHost/Program.cs` to spin up Meilisearch container and wire references.
4. Implement `IAuditLogRepository`, `MeilisearchAuditLogRepository`, and `MeilisearchIndexInitializer` in `Palantir.AuditService`.
5. Update `AuditEndpoints.cs` and `Program.cs` in `Palantir.AuditService`.
6. Update `AdminApp.tsx` in `admin-mfe`.
7. Update documentation and `.insight` architecture bootstrap files.
Do not run `dotnet build` or `dotnet run`.
```
