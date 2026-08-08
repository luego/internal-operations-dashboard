using InternalOperations.Application.Features.Dashboard;

namespace InternalOperations.Application.Abstractions.Persistence;

public interface IDashboardQueryService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);
    Task<DashboardTrendsDto> GetTrendsAsync(int days, CancellationToken cancellationToken);
}
