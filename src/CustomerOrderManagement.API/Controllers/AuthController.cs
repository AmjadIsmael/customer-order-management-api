using Asp.Versioning;
using CustomerOrderManagement.API.RateLimiting;
using CustomerOrderManagement.Business.Interfaces.Services;
using CustomerOrderManagement.Domain.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CustomerOrderManagement.API.Controllers;

/// <summary>
/// Authenticates users and issues the JWT bearer tokens required to call the other endpoints,
/// and lets new customers self-register.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Exchanges a username and password for a JWT access token.
    /// </summary>
    /// <param name="dto">The username and password to authenticate with.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <remarks>
    /// This endpoint is anonymous but rate-limited separately from the rest of the API
    /// (see the <c>RateLimiting:Auth</c> configuration section) to slow down credential
    /// stuffing attempts. Paste the returned <c>accessToken</c> into Swagger's Authorize
    /// dialog (no "Bearer " prefix needed) to call the protected endpoints.
    /// </remarks>
    /// <response code="200">Authentication succeeded; a bearer token is returned.</response>
    /// <response code="400">The request body failed validation.</response>
    /// <response code="401">The username or password is incorrect, or the account is deactivated.</response>
    /// <response code="429">Too many login attempts; retry after the window in the <c>Retry-After</c> header.</response>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicyName)]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(dto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Self-registers as a customer: creates a linked customer profile and login account,
    /// then returns a bearer token as if you'd just logged in.
    /// </summary>
    /// <param name="dto">The account credentials and customer profile details.</param>
    /// <param name="cancellationToken">Propagates request cancellation.</param>
    /// <remarks>
    /// This endpoint is anonymous but rate-limited separately from the rest of the API
    /// (see the <c>RateLimiting:Auth</c> configuration section). The resulting account has
    /// the <c>Customer</c> role, which can read and update only its own customer record and
    /// view only its own orders.
    /// </remarks>
    /// <response code="201">Registration succeeded; a bearer token is returned.</response>
    /// <response code="400">The request body failed validation.</response>
    /// <response code="409">The username or email is already in use.</response>
    /// <response code="429">Too many attempts; retry after the window in the <c>Retry-After</c> header.</response>
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicyName)]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponseDto>> Register(
        [FromBody] RegisterRequestDto dto,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(dto, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
