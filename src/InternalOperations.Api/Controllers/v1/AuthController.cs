using System.ComponentModel.DataAnnotations;
using InternalOperations.Api.ErrorHandling;
using InternalOperations.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InternalOperations.Api.Controllers.v1;

[Route("api/v1/auth")]
[ApiController]
[AllowAnonymous]
[RequestSizeLimit(16 * 1024)]
[Consumes("application/json")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType<TokenPairResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
        => (await sender.Send(new LoginCommand(request.Identifier, request.Password, request.DeviceDescription), cancellationToken)).ToActionResult();

    [HttpPost("refresh")]
    [EnableRateLimiting("auth-refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
        => (await sender.Send(new RefreshSessionCommand(request.RefreshToken, request.DeviceDescription), cancellationToken)).ToActionResult();

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return result.IsSuccess ? NoContent() : StatusCode(500);
    }
}

public sealed record LoginRequest([Required, MaxLength(320)] string Identifier, [Required, MaxLength(1024)] string Password, [MaxLength(200)] string? DeviceDescription);
public sealed record RefreshTokenRequest([Required, MaxLength(1024)] string RefreshToken, [MaxLength(200)] string? DeviceDescription);
public sealed record LogoutRequest([Required, MaxLength(1024)] string RefreshToken);
