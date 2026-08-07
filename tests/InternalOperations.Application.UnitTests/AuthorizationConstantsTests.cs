using InternalOperations.Application.Common.Authorization;

namespace InternalOperations.Application.UnitTests;

public sealed class AuthorizationConstantsTests
{
    [Fact]
    public void ApprovedRolePolicyMatrixIsStable()
    {
        Assert.Equal(6, AuthorizationPolicies.All.Count);
        Assert.Equal(4, ApplicationRoles.All.Count);
        Assert.Contains(AuthorizationPolicies.UsersManage, AuthorizationPolicies.ForRole(ApplicationRoles.Administrator));
        Assert.DoesNotContain(AuthorizationPolicies.UsersManage, AuthorizationPolicies.ForRole(ApplicationRoles.Manager));
        Assert.Equal([AuthorizationPolicies.TicketsRead, AuthorizationPolicies.DashboardRead], AuthorizationPolicies.ForRole(ApplicationRoles.Viewer));
    }
}
