---
insight_schema: "1.0.0"
feature_id: "phase-1-building-blocks"
standards_version: "1.0.0"
source: "docs/review.md"
spec_hash: "8c17e60fdeb8e2d9631786b7f83147c5ec908fc43309456ac6c907b23a8baefc"
status: "approved"
---

# Feature Implementation Plan: Phase 1 - Build Repair & Core Building Blocks

## 1. Requirements Matrix
* **[REQ-01]**: Fix testing dependencies in `tests/Palantir.ContractTests/Palantir.ContractTests.csproj` by adding `xunit` and `xunit.runner.visualstudio` package references.
* **[REQ-02]**: Implement strongly-typed `ClientId` value object using Vogen (`[ValueObject<Guid>]`) in `src/BuildingBlocks/Palantir.SharedKernel/Identifiers.cs` with validation rejecting `Guid.Empty`.
* **[REQ-03]**: Enhance Roslyn source generator `QueryBinderGenerator` in `src/BuildingBlocks/Palantir.Generators/QueryBinderGenerator.cs` to inspect `[QueryParamName]` attributes from `Palantir.Generators.Abstractions` on query model properties, falling back to lowercase property name when omitted.
* **[REQ-04]**: Replace humorous `exciting` method in `src/BuildingBlocks/Palantir.SharedKernel/TradingValueObjects.cs` with standard, expressive exception factory `ThrowCurrencyMismatchException`.
* **[REQ-05]**: Ensure `Palantir.Platform` caching extensions register a System.Text.Json serializer for FusionCache (`WithSerializer(...)`) to support Redis L2 distributed backplane without runtime deserialization faults.
* **[REQ-06]**: Standardize WolverineFx integration helpers in `Palantir.Platform` to provide clean IMessageBus extension methods adhering to SOLID principles.

## 2. In-Scope Modules & File Targets
* `tests/Palantir.ContractTests/Palantir.ContractTests.csproj` (Modify: Add xunit and visualstudio test runner package references)
* `src/BuildingBlocks/Palantir.SharedKernel/Identifiers.cs` (Modify: Add ClientId strongly-typed ID struct)
* `src/BuildingBlocks/Palantir.SharedKernel/TradingValueObjects.cs` (Modify: Replace exciting() with ThrowCurrencyMismatchException)
* `src/BuildingBlocks/Palantir.Generators/QueryBinderGenerator.cs` (Modify: Support [QueryParamName] attribute in generator syntax tree inspection)
* `src/BuildingBlocks/Palantir.Platform/Caching/CachingExtensions.cs` (Modify: Add FusionCache JSON serializer registration)

## 3. Explicit Out-of-Scope (Scope Guard)
* Modifications to Aspire AppHost microservice registrations (deferred to Phase 2).
* Orleans grain storage and clustering configuration (deferred to Phase 3).
* TimescaleDB continuous aggregate DDL scripts (deferred to Phase 4).
* Elasticsearch client integration (deferred to Phase 5).

## 4. Architectural Invariants
* **[ARCH-01]**: `Palantir.SharedKernel` must maintain zero inward dependencies on `Palantir.Platform` or external framework packages.
* **[ARCH-02]**: All domain identifiers must use Vogen strongly-typed value objects with immutability and compile-time validation.
* **[ARCH-03]**: Roslyn source generators must emit allocation-free, reflectionless boilerplate compatible with Native AOT compilation.

## 5. Verification & Test Plan
* **Unit Tests**: `tests/Palantir.ArchTests/ArchitectureTests.cs` verifying layer boundary isolation and entity type constraints.
* **Contract Tests**: `tests/Palantir.ContractTests/OmsRiskContractTests.cs` verifying contract test suite compilation.
