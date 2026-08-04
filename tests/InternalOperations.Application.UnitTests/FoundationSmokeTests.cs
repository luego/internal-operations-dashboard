using InternalOperations.Application;

namespace InternalOperations.Application.UnitTests;

public sealed class FoundationSmokeTests
{
    [Fact]
    public void ApplicationAssemblyIsDiscoverable()
    {
        Assert.Equal("InternalOperations.Application", typeof(ApplicationAssemblyMarker).Assembly.GetName().Name);
    }
}
