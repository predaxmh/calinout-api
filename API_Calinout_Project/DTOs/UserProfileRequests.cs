using System.ComponentModel.DataAnnotations;

namespace API_Calinout_Project.DTOs
{

    public record UserProfileResponseDto(
         string UserId,
         string? FirstName,
         string? LastName,
         int? Gender,
         decimal? HeightInCm,
         decimal? WeightInKg,
         string? MeasurementSystem,
        string? Email,
        DateTime? BirthDate
    );

    public class UpdateUserProfileRequestDto
    {

        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public int? Gender { get; set; }
        public decimal? HeightInCm { get; set; }
        public decimal? WeightInKg { get; set; }

        public string? MeasurementSystem { get; set; }
        public string? Email { get; set; }
        public DateTime? BirthDate { get; set; }
    }
}