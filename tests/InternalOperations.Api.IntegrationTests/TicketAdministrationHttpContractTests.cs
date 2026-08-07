using InternalOperations.Api.Controllers.v1;
using InternalOperations.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalOperations.Api.IntegrationTests;

public sealed class TicketAdministrationHttpContractTests
{
    [Fact]
    public void TicketsControllerPublishesCreateAndGetWithoutClientControlledNumber()
    {
        var type = typeof(TicketsController);
        var methods = type.GetMethods().Where(method => method.DeclaringType == type).ToArray();

        Assert.Equal(2, methods.Length);
        var create = methods.Single(method => method.Name == "Create");
        var authorize = Assert.Single(create.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(AuthorizationPolicies.TicketsCreate, authorize.Policy);
        Assert.NotNull(methods.Single(method => method.Name == "Get").GetCustomAttributes(typeof(HttpGetAttribute), true).SingleOrDefault());
        Assert.DoesNotContain(typeof(CreateTicketRequest).GetProperties(), property => property.Name == "Number");
    }
}
