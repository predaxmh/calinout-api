using System.ComponentModel.DataAnnotations;

namespace API_Calinout_Project.DTOs
{
    public class CreateDailyLogRequest
    {

        [Required]
        public DateTime Date { get; set; }

        [Range(0, 100000)]
        public int BurnedCalories { get; set; }

        [Range(0, 999.99)]
        public decimal? WeightAtLog { get; set; }

        public bool DigestiveTrackCleared { get; set; } = false;

        public bool IsCheatDay { get; set; } = false;

        [Range(0, 100000)]
        public int? TargetCalorieOnThisDay { get; set; }

        [MaxLength(2000)]
        public string? DailyNotes { get; set; }

        [Range(0, 99999.99)]
        public int TotalCalories { get; set; }

        [Range(0, 99999.99)]
        public decimal TotalFoodWeight { get; set; }

        [Range(0, 99999.99)]
        public decimal TotalFat { get; set; }

        [Range(0, 99999.99)]
        public decimal TotalCarbs { get; set; }

        [Range(0, 99999.99)]
        public decimal TotalProtein { get; set; }
    }

    public class UpdateDailyLogRequest
    {

        [Range(0, 100000)]
        public int? BurnedCalories { get; set; }

        [Range(0, 999.99)]
        public decimal? WeightAtLog { get; set; }

        public bool? DigestiveTrackCleared { get; set; }

        public bool? IsCheatDay { get; set; }

        [Range(0, 100000)]
        public int? TargetCalorieOnThisDay { get; set; }

        [MaxLength(2000)]
        public string? DailyNotes { get; set; }

    }

    public record DailyLogResponse(
        int Id,
        string UserId,
        DateTime Date,
        int? BurnedCalories,
        decimal? WeightAtLog,
        bool DigestiveTrackCleared,
        bool IsCheatDay,
        int? TargetCalorieOnThisDay,
        string? DailyNotes,
        int? TotalCalories,
        decimal? TotalFoodWeight,
        decimal? TotalFat,
        decimal? TotalCarbs,
        decimal? TotalProtein,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}