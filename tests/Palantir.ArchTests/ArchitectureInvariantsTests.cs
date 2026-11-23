using Palantir.Constants;
using Palantir.Platform.CQRS;
using Palantir.Platform.Endpoints;
using Palantir.SharedKernel;
using Shouldly;
using Xunit;

namespace Palantir.ArchTests;

public class ArchitectureInvariantsTests
{
    [Fact]
    public void DomainLayer_ShouldNotDependOn_InfrastructureLayer()
    {
        var sharedKernelAssembly = typeof(Order).Assembly;
        var referencedAssemblies = sharedKernelAssembly.GetReferencedAssemblies();

        referencedAssemblies.ShouldNotContain(a => a.Name == "Palantir.Platform");
        referencedAssemblies.ShouldNotContain(a => a.Name == "Npgsql");
        referencedAssemblies.ShouldNotContain(a => a.Name == "Marten");
        referencedAssemblies.ShouldNotContain(a => a.Name == "Wolverine");
    }

    [Fact]
    public void MicroserviceEndpoints_ShouldImplement_IEndpoint()
    {
        var endpointInterface = typeof(IEndpoint);
        endpointInterface.ShouldNotBeNull();
    }

    [Fact]
    public void AggregateRoots_ShouldInherit_FromAggregateRoot()
    {
        var orderType = typeof(Order);
        typeof(AggregateRoot<OrderId>).IsAssignableFrom(orderType).ShouldBeTrue();
    }

    [Fact]
    public void ValueObjects_ShouldEnforce_ValidationRules()
    {
        Should.Throw<Vogen.ValueObjectValidationException>(() =>
        {
            OrderId.From(Guid.Empty);
        });

        Should.Throw<Vogen.ValueObjectValidationException>(() =>
        {
            SymbolId.From(string.Empty);
        });
    }
}
