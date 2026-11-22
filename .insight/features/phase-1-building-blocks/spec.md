# Specification: Phase 1 - Build Repair & Core Building Blocks

## Requirements Matrix
* **[REQ-01]**: Fix testing dependencies in `tests/Palantir.ContractTests/Palantir.ContractTests.csproj` by adding `xunit` and `xunit.runner.visualstudio` package references.
* **[REQ-02]**: Implement strongly-typed `ClientId` value object using Vogen (`[ValueObject<Guid>]`) in `src/BuildingBlocks/Palantir.SharedKernel/Identifiers.cs` with validation rejecting `Guid.Empty`.
* **[REQ-03]**: Enhance Roslyn source generator `QueryBinderGenerator` in `src/BuildingBlocks/Palantir.Generators/QueryBinderGenerator.cs` to inspect `[QueryParamName]` attributes from `Palantir.Generators.Abstractions` on query model properties, falling back to lowercase property name when omitted.
* **[REQ-04]**: Replace humorous `exciting` method in `src/BuildingBlocks/Palantir.SharedKernel/TradingValueObjects.cs` with standard, expressive exception factory `ThrowCurrencyMismatchException`.
* **[REQ-05]**: Ensure `Palantir.Platform` caching extensions register a System.Text.Json serializer for FusionCache (`WithSerializer(...)`) to support Redis L2 distributed backplane without runtime deserialization faults.
* **[REQ-06]**: Standardize WolverineFx integration helpers in `Palantir.Platform` to provide clean IMessageBus extension methods adhering to SOLID principles.
