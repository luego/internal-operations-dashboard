using InternalOperations.Api.ErrorHandling;
using InternalOperations.Application.Common.Authorization;
using InternalOperations.Application.Features.TicketCollaboration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalOperations.Api.Controllers.v1;

[ApiController]
[Route("api/v1/tickets/{ticketId:guid}")]
public sealed class TicketCollaborationController(ISender sender) : ControllerBase
{
    [HttpPost("comments")]
    [Authorize(Policy = AuthorizationPolicies.TicketsCreate)]
    [ProducesResponseType<TicketCommentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddComment(
        Guid ticketId,
        AddTicketCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AddTicketCommentCommand(ticketId, request.Comment),
            cancellationToken);
        return result.IsSuccess
            ? Created($"/api/v1/tickets/{ticketId}/comments/{result.Value!.Id}", result.Value)
            : result.ToActionResult();
    }

    [HttpGet("comments")]
    [Authorize(Policy = AuthorizationPolicies.TicketsRead)]
    [ProducesResponseType<TicketCommentPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListComments(
        Guid ticketId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new ListTicketCommentsQuery(ticketId, page, pageSize),
            cancellationToken)).ToActionResult();

    [HttpGet("history")]
    [Authorize(Policy = AuthorizationPolicies.TicketsRead)]
    [ProducesResponseType<TicketHistoryPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(
        Guid ticketId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new GetTicketHistoryQuery(ticketId, page, pageSize),
            cancellationToken)).ToActionResult();
}

public sealed record AddTicketCommentRequest(string Comment);
