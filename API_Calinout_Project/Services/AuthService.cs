using API_Calinout_Project.DTOs;

using API_Calinout_Project.Entities;

using API_Calinout_Project.Services.Interfaces;
using API_Calinout_Project.Shared;

using Microsoft.AspNetCore.Identity;


namespace API_Calinout_Project.Services
{
    public class AuthService : IAuthService
    {

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly ILogger<AuthService> _logger;
        private readonly ITokenService _tokenService;
        private readonly IHttpContextAccessor _httpContextAccessor;



        public AuthService(UserManager<ApplicationUser> userManager, ILogger<AuthService> logger, ITokenService tokenService, IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _logger = logger;
            _tokenService = tokenService;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken ct)
        {

            var userExists = await _userManager.FindByEmailAsync(request.Email);
            if (userExists != null)
            {
                _logger.LogWarning("Registration attempt failed: Email {Email} already exists.", request.Email);
                return Result<AuthResponseDto>.Failure("Email Is already in use");
            }

            var user = new ApplicationUser()
            {
                UserName = request.Email,
                Email = request.Email,
                MeasurementSystem = "Metric",
                CreatedAt = DateTime.UtcNow,
            };
            var result = await _userManager.CreateAsync(user, request.Password);


            if (!result.Succeeded)
            {
                var errors = String.Join(" ", result.Errors.Select(e => e.Description));

                _logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, errors);
                return Result<AuthResponseDto>.Failure($"Registration failed: {errors}");
            }

            await _userManager.AddToRoleAsync(user, "User");
            string ipAddress = GetIpAddress();

            return await _tokenService.GenerateTokensAsync(user, ipAddress, ct);
        }

        public async Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken ct)
        {

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {

                _logger.LogWarning("Login failed for {Email}: User not found.", request.Email);
                return Result<AuthResponseDto>.Failure("Invalid email or password.");
            }


            var isValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!isValid)
            {
                _logger.LogWarning("Login failed for {Email}: Invalid password.", request.Email);
                return Result<AuthResponseDto>.Failure("Invalid email or password.");
            }

            // 3. Generate Tokens
            var ipAddress = GetIpAddress();
            return await _tokenService.GenerateTokensAsync(user, ipAddress, ct);

        }

        public async Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken ct = default)
        {
            var ipAddress = GetIpAddress();
            return await _tokenService.RefreshTokenAsync(request.RefreshToken, ipAddress, ct);
        }

        public async Task RevokeTokenAsync(string refreshToken, string ipAddress, CancellationToken ct = default)
        {
            // Just a pass-through
            await _tokenService.RevokeTokenAsync(refreshToken, ipAddress, "User Logout", ct);
        }

        private string GetIpAddress()
        {
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}