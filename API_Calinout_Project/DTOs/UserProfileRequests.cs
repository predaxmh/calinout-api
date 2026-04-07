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
        [MaxLength(24)]
        public string? FirstName { get; set; }
        [MaxLength(24)]
        public string? LastName { get; set; }

        public int? Gender { get; set; }
        [Range(0.01, 300)]
        public decimal? HeightInCm { get; set; }

        [Range(10, 500)]
        public decimal? WeightInKg { get; set; }

        [MaxLength(10)]
        public string? MeasurementSystem { get; set; }

        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; }
        public DateTime? BirthDate { get; set; }
    }
}