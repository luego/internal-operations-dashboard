using InternalOperations.Api.Controllers.v1;
using InternalOperations.Application.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalOperations.Api.IntegrationTests;

public sealed class UserAdministrationHttpContractTests
{
    [Fact]
    public void UsersControllerPublishesSevenAuthorizedEndpoints()
    {
        var type = typeof(UsersController);
        var authorize = Assert.Single(type.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Equal(AuthorizationPolicies.UsersManage, authorize.Policy);

        var methods = type.GetMethods().Where(method => method.DeclaringType == type).ToArray();
        Assert.Equal(7, methods.Length);
        Assert.Contains(methods, method => method.GetCustomAttributes(typeof(HttpGetAttribute), true).Length > 0 && method.Name == "List");
        Assert.Contains(methods, method => method.GetCustomAttributes(typeof(HttpPostAttribute), true).Length > 0);
        Assert.Contains(methods, method => method.GetCustomAttributes(typeof(HttpPutAttribute), true).Length > 0 && method.Name == "Update");
        Assert.Contains(methods, method => method.Name == "SetDepartment");
        Assert.Contains(methods, method => method.Name == "SetStatus");
        Assert.Contains(methods, method => method.Name == "SetRoles");
    }
}
