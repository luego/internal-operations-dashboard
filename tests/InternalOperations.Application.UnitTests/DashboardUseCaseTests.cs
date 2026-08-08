using InternalOperations.Application.Abstractions.Persistence;
using InternalOperations.Application.Features.Dashboard;

namespace InternalOperations.Application.UnitTests;

public sealed class DashboardUseCaseTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public void TrendsValidatorRejectsUnsupportedRange(int days)
    {
        var result = new GetDashboardTrendsQueryValidator().Validate(new GetDashboardTrendsQuery(days));

        Assert.False(result.IsSuccess);
        Assert.Equal("dashboard.days_range", result.Error!.Code);
    }

    [Fact]
    public async Task HandlersDelegateToReadService()
    {
        var service = new FakeDashboardService();

        var summary = await new GetDashboardSummaryQueryHandler(service).Handle(new GetDashboardSummaryQuery(), default);
        var trends = await new GetDashboardTrendsQueryHandler(service).Handle(new GetDashboardTrendsQuery(14), default);

        Assert.Equal(7, summary.TotalTickets);
        Assert.Equal(14, service.RequestedDays);
        Assert.Single(trends.Points);
    }

    private sealed class FakeDashboardService : IDashboardQueryService
    {
        public int RequestedDays { get; private set; }

        public Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new DashboardSummaryDto(
                DateTime.UnixEpoch,
                7,
                2,
                2,
                1,
                2,
                1,
                3,
                2,
                4));

        public Task<DashboardTrendsDto> GetTrendsAsync(int days, CancellationToken cancellationToken)
        {
            RequestedDays = days;
            return Task.FromResult(new DashboardTrendsDto(
                DateOnly.FromDateTime(DateTime.UnixEpoch),
                DateOnly.FromDateTime(DateTime.UnixEpoch),
                [new DashboardTrendPointDto(DateOnly.FromDateTime(DateTime.UnixEpoch), 1, 2)]));
        }
    }
}
