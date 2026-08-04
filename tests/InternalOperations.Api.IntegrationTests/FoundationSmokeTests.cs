using InternalOperations.Api;

namespace InternalOperations.Api.IntegrationTests;

public sealed class FoundationSmokeTests
{
    [Fact]
    public void ApiAssemblyIsDiscoverable()
    {
        Assert.Equal("InternalOperations.Api", typeof(ApiAssemblyMarker).Assembly.GetName().Name);
    }
}
