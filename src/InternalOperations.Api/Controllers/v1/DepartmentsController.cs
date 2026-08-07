using InternalOperations.Api.ErrorHandling;
using InternalOperations.Application.Common.Authorization;
using InternalOperations.Application.Features.Departments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalOperations.Api.Controllers.v1;

[Authorize(Policy = AuthorizationPolicies.UsersManage)]
public sealed class DepartmentsController(ISender sender) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<DepartmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new ListDepartmentsQuery(page, pageSize, search, isActive, sortBy, sortDirection),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [ProducesResponseType<DepartmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateDepartmentCommand(request.Name, request.Description),
            cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value)
            : result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<DepartmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDepartmentQuery(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<DepartmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new UpdateDepartmentCommand(id, request.Name, request.Description, request.Version),
            cancellationToken);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<DepartmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetStatus(
        Guid id,
        SetDepartmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SetDepartmentStatusCommand(id, request.IsActive, request.Version),
            cancellationToken);
        return result.ToActionResult();
    }
}

public sealed record CreateDepartmentRequest(string Name, string? Description);
public sealed record UpdateDepartmentRequest(string Name, string? Description, Guid Version);
public sealed record SetDepartmentStatusRequest(bool IsActive, Guid Version);
