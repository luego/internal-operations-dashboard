using InternalOperations.Api.ErrorHandling;
using InternalOperations.Application.Common.Authorization;
using InternalOperations.Application.DTOs;
using InternalOperations.Application.Features.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalOperations.Api.Controllers.v1;

public sealed class TicketsController : BaseApiController
{
    private readonly ISender _sender;

    public TicketsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.TicketsCreate)]
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
