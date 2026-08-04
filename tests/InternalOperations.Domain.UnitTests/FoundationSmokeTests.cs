using InternalOperations.Domain;

namespace InternalOperations.Domain.UnitTests;

public sealed class FoundationSmokeTests
{
    [Fact]
    public void DomainAssemblyIsDiscoverable()
    {
        Assert.Equal("InternalOperations.Domain", typeof(DomainAssemblyMarker).Assembly.GetName().Name);
    }
}
