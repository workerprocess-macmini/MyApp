using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyApp.Application.Features.Auth;
using MyApp.Application.Features.Auth.Commands.Login;
using MyApp.Application.Features.Auth.Commands.RefreshToken;
using MyApp.Application.Features.Auth.Commands.Register;
using MyApp.Application.Features.Auth.Commands.RevokeToken;

namespace MyApp.API.Controllers;

/// <summary>Authentication — register, login, token refresh, and revoke.</summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Auth")]
[Produces("application/json")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>Register a new user account.</summary>
    /// <param name="command">Registration details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access token, refresh token, and basic profile.</returns>
    [HttpPost("register")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    /// <summary>Authenticate with email and password.</summary>
    /// <param name="command">Login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access token, refresh token, and basic profile.</returns>
    [HttpPost("login")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    /// <summary>Rotate an expired access token using a valid refresh token.</summary>
    /// <param name="command">The current refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New access token and rotated refresh token.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    /// <summary>Revoke a refresh token (logout).</summary>
    /// <param name="command">The refresh token to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [Authorize]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Revoke(
        [FromBody] RevokeTokenCommand command,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}
