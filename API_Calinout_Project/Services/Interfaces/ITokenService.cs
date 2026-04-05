
using API_Calinout_Project.DTOs;
using API_Calinout_Project.Entities;
using API_Calinout_Project.Shared;


namespace API_Calinout_Project.Services.Interfaces
{


    public interface ITokenService
    {
        Task<Result<AuthResponseDto>> GenerateTokensAsync(ApplicationUser user, string ipAddress, CancellationToken ct = default);
        Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken ct = default);
        Task RevokeTokenAsync(string refreshToken, string ipAddress, string reason, CancellationToken ct = default);


    }
}