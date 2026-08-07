using InternalOperations.Api.Controllers.v1;
using InternalOperations.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalOperations.Api.IntegrationTests;

public sealed class TicketCollaborationHttpContractTests
{
    [Fact]
    public void ControllerPublishesThreeAuthorizedOperationsWithoutClientControlledAuthor()
    {
        var type = typeof(TicketCollaborationController);
        var route = Assert.Single(type.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>());
        var methods = type.GetMethods().Where(method => method.DeclaringType == type).ToArray();

        Assert.Equal("api/v1/tickets/{ticketId:guid}", route.Template);
        Assert.Equal(3, methods.Length);
        AssertPolicy(methods.Single(method => method.Name == "AddComment"), AuthorizationPolicies.TicketsCreate);
        AssertPolicy(methods.Single(method => method.Name == "ListComments"), AuthorizationPolicies.TicketsRead);
        AssertPolicy(methods.Single(method => method.Name == "GetHistory"), AuthorizationPolicies.TicketsRead);
        Assert.DoesNotContain(typeof(AddTicketCommentRequest).GetProperties(), property => property.Name.Contains("Author", StringComparison.Ordinal));
    }

    private static void AssertPolicy(System.Reflection.MethodInfo method, string expectedPolicy)
    {
        var authorize = Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(expectedPolicy, authorize.Policy);
    }
}
