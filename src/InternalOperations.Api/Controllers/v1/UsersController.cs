using InternalOperations.Api.ErrorHandling;
using InternalOperations.Application.Common.Authorization;
using InternalOperations.Application.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternalOperations.Api.Controllers.v1;

[Authorize(Policy = AuthorizationPolicies.UsersManage)]
public sealed class UsersController(ISender sender) : BaseApiController
{
    [HttpGet]
    [ProducesResponseType<UserPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] Guid? departmentId = null,
        [FromQuery] bool? hasDepartment = null,
        [FromQuery] string? role = null,
        [FromQuery] string sortBy = "userName",
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default) =>
        (await sender.Send(new ListUsersQuery(page, pageSize, search, isActive, departmentId, hasDepartment, role, sortBy, sortDirection), cancellationToken)).ToActionResult();

    [HttpPost]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateUserCommand(
            request.UserName, request.Email, request.DisplayName, request.InitialPassword, request.Roles, request.DepartmentId), cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) =>
        (await sender.Send(new GetUserQuery(id), cancellationToken)).ToActionResult();

    [HttpPut("{id:guid}")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, UpdateUserRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new UpdateUserCommand(id, request.UserName, request.Email, request.DisplayName, request.Version), cancellationToken)).ToActionResult();

    [HttpPatch("{id:guid}/department")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetDepartment(Guid id, SetUserDepartmentRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new SetUserDepartmentCommand(id, request.DepartmentId, request.Version), cancellationToken)).ToActionResult();

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetStatus(Guid id, SetUserStatusRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new SetUserStatusCommand(id, request.IsActive, request.Version), cancellationToken)).ToActionResult();

    [HttpPut("{id:guid}/roles")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetRoles(Guid id, SetUserRolesRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(new SetUserRolesCommand(id, request.Roles, request.Version), cancellationToken)).ToActionResult();
}

public sealed record CreateUserRequest(
    string UserName,
    string Email,
    string DisplayName,
    string InitialPassword,
    IReadOnlyList<string> Roles,
    Guid? DepartmentId);
public sealed record UpdateUserRequest(string UserName, string Email, string DisplayName, Guid Version);
public sealed record SetUserDepartmentRequest(Guid? DepartmentId, Guid Version);
public sealed record SetUserStatusRequest(bool IsActive, Guid Version);
public sealed record SetUserRolesRequest(IReadOnlyList<string> Roles, Guid Version);
