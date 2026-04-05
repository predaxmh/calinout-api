namespace API_Calinout_Project.Entities
{
    /// <summary>
    /// Represents a daily log entry for a user's health and nutrition tracking, including calorie intake, weight, and
    /// related notes for a specific date.
    /// </summary>
    /// <remarks>A DailyLog instance records various metrics and notes for a single day, associated with a
    /// specific user. It can be used to track dietary habits, exercise, and other health-related information over time.
    /// This class is typically used in applications that monitor user wellness or fitness progress.</remarks>
    public class DailyLog
    {
        public int Id { get; set; }
        public required string UserId { get; set; }
        public DateTime Date { get; set; }

        public int? BurnedCalories { get; set; }
        public decimal? WeightAtLog { get; set; }
        public bool DigestiveTrackCleared { get; set; }
        public bool IsCheatDay { get; set; }
        public int? TargetCalorieOnThisDay { get; set; }
        public string? DailyNotes { get; set; }

        public int? TotalCalories { get; set; }
        public decimal? TotalFoodWeight { get; set; }
        public decimal? TotalFat { get; set; }
        public decimal? TotalCarbs { get; set; }
        public decimal? TotalProtein { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ApplicationUser? User { get; set; }

    }
}