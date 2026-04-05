using Microsoft.AspNetCore.Identity;

namespace API_Calinout_Project.Entities
{

    public enum Gender
    {
        Male = 0,
        Female = 1,
    }
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public DateOnly? DateOfBirth { get; set; }
        public Gender? Gender { get; set; }
        public decimal? HeightInCm { get; set; }
        public decimal? WeightInKg { get; set; }

        // Preference: "Metric" or "Imperial"
        public string MeasurementSystem { get; set; } = "Metric";

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new HashSet<RefreshToken>();

        public ICollection<Food> Foods { get; set; } = new HashSet<Food>();
        public ICollection<Meal> Meals { get; set; } = new HashSet<Meal>();

        public ICollection<DailyLog> DailyLogs { get; set; } = new HashSet<DailyLog>();

    }
}