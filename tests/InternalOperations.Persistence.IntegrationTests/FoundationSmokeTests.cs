using InternalOperations.Persistence;

namespace InternalOperations.Persistence.IntegrationTests;

public sealed class FoundationSmokeTests
{
    [Fact]
    public void PersistenceAssemblyIsDiscoverable()
    {
        Assert.Equal("InternalOperations.Persistence", typeof(PersistenceAssemblyMarker).Assembly.GetName().Name);
    }
}
