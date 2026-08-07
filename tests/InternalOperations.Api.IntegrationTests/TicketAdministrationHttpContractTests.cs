using InternalOperations.Api.Controllers.v1;
using InternalOperations.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalOperations.Api.IntegrationTests;

public sealed class TicketAdministrationHttpContractTests
{
    [Fact]
    public void TicketsControllerPublishesFiveAuthorizedOperationsWithoutClientControlledNumber()
    {
        var type = typeof(TicketsController);
        var methods = type.GetMethods().Where(method => method.DeclaringType == type).ToArray();

        Assert.Equal(5, methods.Length);
        AssertPolicy(methods.Single(method => method.Name == "Create"), AuthorizationPolicies.TicketsCreate);
        AssertPolicy(methods.Single(method => method.Name == "Get"), AuthorizationPolicies.TicketsRead);
        AssertPolicy(methods.Single(method => method.Name == "List"), AuthorizationPolicies.TicketsRead);
        AssertPolicy(methods.Single(method => method.Name == "Update"), AuthorizationPolicies.TicketsAssign);
        AssertPolicy(methods.Single(method => method.Name == "ChangeStatus"), AuthorizationPolicies.TicketsChangeStatus);
        Assert.DoesNotContain(typeof(CreateTicketRequest).GetProperties(), property => property.Name == "Number");
    }

    private static void AssertPolicy(System.Reflection.MethodInfo method, string expectedPolicy)
    {
        var authorize = Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(expectedPolicy, authorize.Policy);
    }
}
