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
    [Authorize(Policy = AuthorizationPolicies.TicketsRead)]
    [ProducesResponseType<TicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetTicketQuery(id), cancellationToken)).ToActionResult();

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.TicketsRead)]
    [ProducesResponseType<TicketPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string sortBy = "createdAtUtc",
        [FromQuery] string sortDirection = "desc",
        CancellationToken cancellationToken = default) =>
        (await sender.Send(
            new ListTicketsQuery(
                page,
                pageSize,
                search,
                status,
                priority,
                departmentId,
                userId,
                sortBy,
                sortDirection),
            cancellationToken)).ToActionResult();

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.TicketsAssign)]
    [ProducesResponseType<TicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateTicketRequest request,
        CancellationToken cancellationToken) =>
        (await sender.Send(new UpdateTicketCommand(
            id,
            request.Title,
            request.Description,
            request.Priority,
            request.DepartmentId,
            request.UserId,
            request.Version), cancellationToken)).ToActionResult();

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = AuthorizationPolicies.TicketsChangeStatus)]
    [ProducesResponseType<TicketDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        ChangeTicketStatusRequest request,
        CancellationToken cancellationToken) =>
        (await sender.Send(
            new ChangeTicketStatusCommand(id, request.Status, request.Version),
            cancellationToken)).ToActionResult();
}

public sealed record CreateTicketRequest(
    string Title,
    string Description,
    TicketPriority Priority,
    Guid DepartmentId,
    Guid? UserId);

public sealed record UpdateTicketRequest(
    string Title,
    string Description,
    TicketPriority Priority,
    Guid DepartmentId,
    Guid? UserId,
    Guid Version);

public sealed record ChangeTicketStatusRequest(TicketStatus Status, Guid Version);
