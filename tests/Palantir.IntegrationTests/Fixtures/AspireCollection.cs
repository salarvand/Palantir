using Xunit;

namespace Palantir.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class AspireCollection : ICollectionFixture<AspireAppFixture>
{
    public const string Name = "Aspire";
}
