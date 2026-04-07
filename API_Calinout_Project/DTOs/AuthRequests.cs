using System.ComponentModel.DataAnnotations;

namespace API_Calinout_Project.DTOs
{


    // C# properties should be PascalCase. 
    // ASP.NET Core automatically converts them to camelCase JSON (accessToken) for the frontend.
    public record AuthResponseDto(
        string AccessToken,
        string RefreshToken,
        DateTime AccessTokenExpiresAt
    );

    public class RegisterRequestDto
    {
        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required, MinLength(6), MaxLength(14)]
        public required string Password { get; set; }

        [Compare(nameof(Password))]
        public required string ConfirmPassword { get; set; }
    }

    public class LoginRequestDto
    {
        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required, MinLength(6), MaxLength(14)]
        public required string Password { get; set; }
    }

    public class RefreshTokenRequestDto
    {
        [Required, MinLength(12), MaxLength(90)]
        public required string RefreshToken { get; set; }
    }

}