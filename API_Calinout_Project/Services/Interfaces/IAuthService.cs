using API_Calinout_Project.DTOs;
using API_Calinout_Project.Shared;


namespace API_Calinout_Project.Services.Interfaces
{
    public interface IAuthService
    {
        Task<Result<AuthResponseDto>> RegisterAsync(RegisterRequestDto request, CancellationToken ct = default);

        Task<Result<AuthResponseDto>> LoginAsync(LoginRequestDto request, CancellationToken ct = default);

        Task<Result<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken ct = default);

        Task RevokeTokenAsync(string refreshToken, string ipAddress, CancellationToken ct = default);
    }
}