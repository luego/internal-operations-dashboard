using InternalOperations.Api.Controllers.v1;
using InternalOperations.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalOperations.Api.IntegrationTests;

public sealed class DashboardHttpContractTests
{
    [Fact]
    public void ControllerPublishesAuthorizedSummaryAndTrendsRoutes()
    {
        var controller = typeof(DashboardController);
        Assert.Equal("api/v1/dashboard", controller.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template);
        var methods = controller.GetMethods().Where(method => method.DeclaringType == controller).ToArray();

        AssertEndpoint(methods, nameof(DashboardController.Summary), "summary");
        AssertEndpoint(methods, nameof(DashboardController.Trends), "trends");
    }

    private static void AssertEndpoint(IEnumerable<System.Reflection.MethodInfo> methods, string name, string route)
    {
        var method = Assert.Single(methods, candidate => candidate.Name == name);
        Assert.Equal(route, method.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>().Single().Template);
        Assert.Equal(
            AuthorizationPolicies.DashboardRead,
            method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single().Policy);
    }
}
