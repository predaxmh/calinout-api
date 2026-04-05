using API_Calinout_Project.Data;
using API_Calinout_Project.DTOs;
using API_Calinout_Project.Entities;
using API_Calinout_Project.Services.Interfaces;
using API_Calinout_Project.Settings;
using API_Calinout_Project.Shared;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace API_Calinout_Project.Services
{

    public class TokenService : ITokenService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<TokenService> _logger;
        private readonly byte[] _key;

        public TokenService(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtOptions, ILogger<TokenService> logger)
        {
            _context = db;
            _userManager = userManager;
            _jwtSettings = jwtOptions.Value; // Strongly Typed Config
            _logger = logger;
            _key = Encoding.UTF8.GetBytes(_jwtSettings.Key);
        }

        public async Task<Result<AuthResponseDto>> GenerateTokensAsync(ApplicationUser user, string ipAddress, CancellationToken ct = default)
        {
            // 1. Generate Access Token (JWT)
            var (accessToken, accessExpires) = await CreateJwtAsync(user);

            // 2. Generate Refresh Token (Random String)
            var refreshToken = GenerateSecureString();

            var refreshTokenHash = HashToken(refreshToken);

            // 3. Save to DB
            var entity = new RefreshToken
            {
                TokenHash = refreshTokenHash,

                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress,
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.DurationInMinutes),
            };


            await _context.RefreshTokens.AddAsync(entity, ct);
            await _context.SaveChangesAsync(ct);

            var JwtToken = new AuthResponseDto(accessToken, refreshToken, accessExpires);
            return Result<AuthResponseDto>.Success(JwtToken);

        }

        public async Task<Result<AuthResponseDto>> RefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken ct = default)
        {

            var refreshTokenHash = HashToken(refreshToken);
            var now = DateTime.UtcNow;

            // Use execution strategy for retry logic
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync(ct);

                try
                {
                    // Query with proper locking to prevent race conditions
                    var existingToken = await _context.RefreshTokens
                        .Include(x => x.User)
                        .FirstOrDefaultAsync(x => x.TokenHash == refreshTokenHash, ct);


                    if (existingToken == null)
                    {
                        return Result<AuthResponseDto>.Failure("Invalid refresh token");
                    }

                    // Check if already revoked
                    if (existingToken.IsRevoked || existingToken.IsExpired)
                    {
                        _logger.LogWarning("Security Alert: Attempted reuse of revoked/expired token. UserId: {userId}, IP: {ip}",
                            existingToken.UserId, ipAddress);

                        return Result<AuthResponseDto>.Failure("Invalid Token");
                    }


                    // Rotate the refresh token
                    existingToken.Revoked = DateTime.UtcNow;
                    existingToken.RevokedByIp = ipAddress;
                    existingToken.ReasonRevoked = "Rotated";

                    // Generate new token
                    var newRefreshToken = GenerateSecureString();
                    var newRefreshTokenHash = HashToken(newRefreshToken);

                    // Add jitter to prevent thundering herd(production)
                    var jitterSeconds = Random.Shared.Next(-30, 30);
                    var expiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDurationInDays);

                    var newTokenEntity = new RefreshToken
                    {
                        TokenHash = newRefreshTokenHash,
                        UserId = existingToken.UserId,
                        CreatedByIp = ipAddress,
                        Created = DateTime.UtcNow,
                        Expires = expiresAt.AddSeconds(jitterSeconds),
                    };

                    existingToken.ReplacedByTokenHash = newRefreshTokenHash;


                    await _context.RefreshTokens.AddAsync(newTokenEntity);


                    await _context.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);

                    // Generate new JWT
                    var (jwt, jwtExpiresAt) = await CreateJwtAsync(existingToken.User);



                    var JwtToken = new AuthResponseDto(jwt, newRefreshToken, jwtExpiresAt);
                    return Result<AuthResponseDto>.Success(JwtToken);
                }

                catch (Exception ex)
                {
                    await transaction.RollbackAsync(ct);
                    _logger.LogError(ex, "Refresh token transaction failed for IP {Ip}", ipAddress);
                    return Result<AuthResponseDto>.Failure("Server error during refresh");

                }
            });
        }

        private async Task<(string Token, DateTime Expires)> CreateJwtAsync(ApplicationUser user)
        {

            var roles = await _userManager.GetRolesAsync(user);
            var userClaims = await _userManager.GetClaimsAsync(user);

            var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            //unique token id (helps with tracing/blacklisting)
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("firstName", user.FirstName ?? ""),
        };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            claims.AddRange(userClaims);


            var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.DurationInMinutes);
            var creds = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);


            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                SigningCredentials = creds,
                Expires = expires,
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);

            return (tokenHandler.WriteToken(token), expires);

        }

        private static string GenerateSecureString()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        private static string HashToken(string input)
        {
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
        }

        public async Task RevokeTokenAsync(string token, string ipAddress, string reason, CancellationToken ct = default)
        {
            var hash = HashToken(token);
            var existingToken = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash, ct);

            if (existingToken != null && !existingToken.IsRevoked)
            {
                existingToken.Revoked = DateTime.UtcNow;
                existingToken.RevokedByIp = ipAddress;
                existingToken.ReasonRevoked = reason;
                await _context.SaveChangesAsync(ct);
            }
        }
    }
}