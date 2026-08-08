namespace InternalOperations.Application.Features.Dashboard;

public sealed record DashboardSummaryDto(
    DateTime GeneratedAtUtc,
    int TotalTickets,
    int OpenTickets,
    int InProgressTickets,
    int ResolvedTickets,
    int ClosedTickets,
    int UnassignedTickets,
    int HighPriorityActiveTickets,
    int ActiveDepartments,
    int ActiveUsers);

public sealed record DashboardTrendPointDto(
    DateOnly Date,
    int TicketsCreated,
    int CommentsAdded);

public sealed record DashboardTrendsDto(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<DashboardTrendPointDto> Points);

public sealed record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public sealed record GetDashboardTrendsQuery(int Days = 30) : IRequest<DashboardTrendsDto>;

public static class DashboardErrors
{
    public static readonly Error InvalidDays = Error.Validation(
        "dashboard.days_range",
        "Days must be between 1 and 90.");
}

public sealed class GetDashboardTrendsQueryValidator : IRequestValidator<GetDashboardTrendsQuery>
{
    public Result Validate(GetDashboardTrendsQuery request) =>
        request.Days is < 1 or > 90
            ? Result.Failure(DashboardErrors.InvalidDays)
            : Result.Success();
}
