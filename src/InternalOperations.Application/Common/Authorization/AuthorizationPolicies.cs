namespace InternalOperations.Application.Common.Authorization;

public static class ApplicationRoles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string Agent = "Agent";
    public const string Viewer = "Viewer";
    public static IReadOnlyList<string> All { get; } = [Administrator, Manager, Agent, Viewer];
}

public static class AuthorizationPolicies
{
    public const string TicketsRead = "Tickets.Read";
    public const string TicketsCreate = "Tickets.Create";
    public const string TicketsAssign = "Tickets.Assign";
    public const string TicketsChangeStatus = "Tickets.ChangeStatus";
    public const string UsersManage = "Users.Manage";
    public const string DashboardRead = "Dashboard.Read";
    public static IReadOnlyList<string> All { get; } = [TicketsRead, TicketsCreate, TicketsAssign, TicketsChangeStatus, UsersManage, DashboardRead];

    public static IReadOnlyList<string> ForRole(string role) => role switch
    {
        ApplicationRoles.Administrator => All,
        ApplicationRoles.Manager => [TicketsRead, TicketsCreate, TicketsAssign, TicketsChangeStatus, DashboardRead],
        ApplicationRoles.Agent => [TicketsRead, TicketsCreate, TicketsChangeStatus],
        ApplicationRoles.Viewer => [TicketsRead, DashboardRead],
        _ => [],
    };
}
