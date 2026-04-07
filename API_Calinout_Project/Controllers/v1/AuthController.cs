using API_Calinout_Project.DTOs;
using API_Calinout_Project.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API_Calinout_Project.Controllers.V1
{
    [EnableRateLimiting("LoginPolicy")]
    [ApiController]
    [Route("api/V1/[controller]")]
    [Produces("application/json")]
    public class AuthController : BaseApiController
    {

        private readonly IAuthService _authService;


        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Registers a new user and returns their initial session tokens.
        /// </summary>
        /// <response code="200">Registration successful, tokens returned.</response>
        /// <response code="400">Validation failed or email already exists.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]

        public async Task<ActionResult<AuthResponseDto>> Register(
            [FromBody] RegisterRequestDto request,
            CancellationToken ct = default)
        {

            var result = await _authService.RegisterAsync(request, ct);

            return HandleResult(result);
        }

        /// <summary>
        /// Logs in an existing user.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]

        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken ct)
        {

            var result = await _authService.LoginAsync(request, ct);

            return HandleResult(result);
        }


        /// <summary>
        /// Refreshes the Access Token using a valid Refresh Token.
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] RefreshTokenRequestDto request, CancellationToken ct)
        {

            var result = await _authService.RefreshTokenAsync(request, ct);

            return HandleResult(result);
        }

        /// <summary>
        /// Revokes the provided Refresh Token (Logout).
        /// Requires the user to be authenticated (Valid Access Token).
        /// </summary>
        [Authorize] // Protected Endpoint
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request, CancellationToken ct)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // We fire and forget (or await if you want to ensure DB write)
            // Even if the token is already invalid, we just say "OK" (204) to the client.
            await _authService.RevokeTokenAsync(request.RefreshToken, ip, ct);

            return NoContent();
        }


    }
}