using InternalOperations.Api.ErrorHandling;
using InternalOperations.Application.Common.Authorization;
using InternalOperations.Application.Features.Tickets;
using InternalOperations.Domain.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalOperations.Api.Controllers.v1;

public sealed class TicketsController(ISender sender) : BaseApiController
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.TicketsCreate)]
    [ProducesResponseType<TicketDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateTicketCommand(
            request.Title,
            request.Description,
            request.Priority,
            request.DepartmentId,
            request.UserId), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value)
            : result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<TicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetTicketQuery(id), cancellationToken)).ToActionResult();
}

public sealed record CreateTicketRequest(
    string Title,
    string Description,
    TicketPriority Priority,
    Guid DepartmentId,
    Guid? UserId);
