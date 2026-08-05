using InternalOperations.Api.ErrorHandling;
using InternalOperations.Application.DTOs;
using InternalOperations.Application.Features.Tickets;
using Microsoft.AspNetCore.Mvc;

namespace InternalOperations.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public sealed class TicketsController : ControllerBase
{
    private readonly ISender _sender;

    public TicketsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateTicketDto request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateTicketCommand(request),
            cancellationToken);

        return result.ToActionResult();
    }
}