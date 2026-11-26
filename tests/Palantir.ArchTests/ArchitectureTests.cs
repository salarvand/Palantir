using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using FluentValidation;
using Palantir.Constants;
using Palantir.Platform.Endpoints;
using Palantir.SharedKernel;
using Shouldly;
using Wolverine;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Palantir.ArchTests;

public sealed class ArchitectureTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(IEntity<>).Assembly,
            typeof(Permissions).Assembly,
            typeof(IEndpoint).Assembly,
            typeof(Palantir.OmsService.Projections.OrderBlotterProjection).Assembly,
            typeof(Palantir.IamService.Domain.TradingProfile).Assembly,
            typeof(Palantir.LocalEngineService.Domain.TradingPair).Assembly,
            typeof(Palantir.ExternalGatewayService.Connectors.IExchangeVenueConnector).Assembly,
            typeof(Palantir.MarketDataService.Hubs.MarketDataHub).Assembly,
            typeof(Palantir.RiskService.Engine.RiskEngine).Assembly,
            typeof(Palantir.PortfolioService.Domain.Account).Assembly,
            typeof(Palantir.SettlementService.Sagas.OrderSettlementSaga).Assembly,
            typeof(Palantir.AuditService.Domain.RegulatoryAuditRecord).Assembly,
            typeof(Palantir.AiAssistantService.Agents.TradeCopilotCoordinator).Assembly)
        .Build();

    [Fact]
    public void SharedKernel_Should_Not_DependOn_Platform()
    {
        var sharedKernelRefs = typeof(IEntity<>).Assembly.GetReferencedAssemblies();
        sharedKernelRefs.ShouldNotContain(a => a.Name == "Palantir.Platform");
    }

    [Fact]
    public void Constants_Should_Have_Zero_Dependencies_On_SharedKernel_Or_Platform()
    {
        var constantsRefs = typeof(Permissions).Assembly.GetReferencedAssemblies();
        constantsRefs.ShouldNotContain(a => a.Name == "Palantir.SharedKernel");
        constantsRefs.ShouldNotContain(a => a.Name == "Palantir.Platform");
    }

    [Fact]
    public void Entities_Should_Inherit_From_IEntity()
    {
        var entityTypes = typeof(IEntity<>).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.BaseType is { IsGenericType: true } && t.BaseType.GetGenericTypeDefinition() == typeof(Entity<>))
            .ToList();

        foreach (var type in entityTypes)
        {
            type.GetInterfaces().ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));
        }
    }

    [Fact]
    public void MicroserviceEndpoints_Should_ResideIn_Features_And_BeSealed()
    {
        Classes()
            .That()
            .ImplementInterface(typeof(IEndpoint))
            .And()
            .DoNotResideInNamespaceMatching(@"Palantir\.Platform\..*")
            .Should()
            .ResideInNamespaceMatching(@"Palantir\..*\.Features\..*")
            .AndShould()
            .BeSealed()
            .Because("All service endpoints must follow Vertical Slice Architecture under Features/ and be sealed.")
            .Check(Architecture);
    }

    [Fact]
    public void Microservices_Should_Not_Contain_Legacy_Endpoints_Namespace()
    {
        Types()
            .That()
            .ResideInNamespaceMatching(@"Palantir\..*Service\.Endpoints.*")
            .GetObjects(Architecture)
            .ShouldBeEmpty();
    }

    [Fact]
    public void Validators_Should_BeSealed_And_ResideIn_Features()
    {
        Classes()
            .That()
            .ImplementInterface(typeof(IValidator<>))
            .And()
            .DoNotResideInNamespaceMatching(@"Palantir\.Platform\..*")
            .Should()
            .ResideInNamespaceMatching(@"Palantir\..*\.Features\..*")
            .AndShould()
            .BeSealed()
            .Because("Validators belong inside their vertical slice under Features/ and must be sealed.")
            .Check(Architecture);
    }

    [Fact]
    public void Sagas_Should_ResideIn_SagasNamespace()
    {
        Classes()
            .That()
            .AreAssignableTo(typeof(Saga))
            .Should()
            .ResideInNamespaceMatching(@"Palantir\..*\.Sagas.*")
            .Because("Distributed Wolverine sagas must reside in a dedicated Sagas namespace.")
            .Check(Architecture);
    }

    [Fact]
    public void Microservices_Should_Have_Zero_CrossService_Or_Contracts_Project_References()
    {
        var serviceAssemblies = new[]
        {
            typeof(Palantir.OmsService.Projections.OrderBlotterProjection).Assembly,
            typeof(Palantir.IamService.Domain.TradingProfile).Assembly,
            typeof(Palantir.LocalEngineService.Domain.TradingPair).Assembly,
            typeof(Palantir.ExternalGatewayService.Connectors.IExchangeVenueConnector).Assembly,
            typeof(Palantir.MarketDataService.Hubs.MarketDataHub).Assembly,
            typeof(Palantir.RiskService.Engine.RiskEngine).Assembly,
            typeof(Palantir.PortfolioService.Domain.Account).Assembly,
            typeof(Palantir.SettlementService.Sagas.OrderSettlementSaga).Assembly,
            typeof(Palantir.AuditService.Domain.RegulatoryAuditRecord).Assembly,
            typeof(Palantir.AiAssistantService.Agents.TradeCopilotCoordinator).Assembly
        };

        foreach (var assembly in serviceAssemblies)
        {
            var refs = assembly.GetReferencedAssemblies();
            refs.ShouldNotContain(a => a.Name != null && a.Name.EndsWith(".Contracts", StringComparison.Ordinal));
            refs.ShouldNotContain(a => a.Name != null && a.Name != assembly.GetName().Name && a.Name.EndsWith("Service", StringComparison.Ordinal));
        }
    }
}
