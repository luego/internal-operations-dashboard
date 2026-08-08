using InternalOperations.Application.Common.Authorization;
using InternalOperations.Application.Features.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalOperations.Api.Controllers.v1;

[ApiController]
[Route("api/v1/dashboard")]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet("summary")]
    [Authorize(Policy = AuthorizationPolicies.DashboardRead)]
    [ProducesResponseType<DashboardSummaryDto>(StatusCodes.Status200OK)]
    public Task<DashboardSummaryDto> Summary(CancellationToken cancellationToken) =>
        sender.Send(new GetDashboardSummaryQuery(), cancellationToken);

    [HttpGet("trends")]
    [Authorize(Policy = AuthorizationPolicies.DashboardRead)]
    [ProducesResponseType<DashboardTrendsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<DashboardTrendsDto> Trends(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default) =>
        sender.Send(new GetDashboardTrendsQuery(days), cancellationToken);
}
