using Palantir.Constants;
using Palantir.Platform.Security;
using Palantir.SharedKernel;
using Shouldly;
using Xunit;

namespace Palantir.ArchTests;

public class ArchitectureTests
{
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
            .Where(t => t.IsClass && !t.IsAbstract && t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(Entity<>))
            .ToList();

        foreach (var type in entityTypes)
        {
            type.GetInterfaces().ShouldContain(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));
        }
    }
}
