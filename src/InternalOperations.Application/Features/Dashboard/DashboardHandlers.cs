using InternalOperations.Application.Abstractions.Persistence;

namespace InternalOperations.Application.Features.Dashboard;

public sealed class GetDashboardSummaryQueryHandler(IDashboardQueryService dashboard)
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    public Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken) =>
        dashboard.GetSummaryAsync(cancellationToken);
}

public sealed class GetDashboardTrendsQueryHandler(IDashboardQueryService dashboard)
    : IRequestHandler<GetDashboardTrendsQuery, DashboardTrendsDto>
{
    public Task<DashboardTrendsDto> Handle(
        GetDashboardTrendsQuery request,
        CancellationToken cancellationToken) =>
        dashboard.GetTrendsAsync(request.Days, cancellationToken);
}
