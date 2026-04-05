using System.ComponentModel.DataAnnotations;

namespace API_Calinout_Project.DTOs
{
    public class CreateGoalRequest
    {
        [Required]
        public int Mode { get; set; } // 0=Cut, 1=Maintain, 2=Bulk

        [Range(0, 10000)]
        public int? DailyCalorieGoal { get; set; }

        [Range(0, 999.9)]
        public decimal? TargetWeight { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(0, 999.9)]
        public decimal? ProteinGoal { get; set; }

        [Range(0, 999.9)]
        public decimal? FatGoal { get; set; }

        [Range(0, 999.9)]
        public decimal? CarbGoal { get; set; }
    }

    public class UpdateGoalRequest
    {
        public int? Mode { get; set; }

        [Range(0, 10000)]
        public int? DailyCalorieGoal { get; set; }

        [Range(0, 999.9)]
        public decimal? TargetWeight { get; set; }

        public bool? IsActive { get; set; }

        public DateTime? EndDate { get; set; }

        [Range(0, 999.9)]
        public decimal? ProteinGoal { get; set; }

        [Range(0, 999.9)]
        public decimal? FatGoal { get; set; }

        [Range(0, 999.9)]
        public decimal? CarbGoal { get; set; }
    }

    public record GoalResponse(
        int Id,
        string UserId,
        int Mode,
        int? DailyCalorieGoal,
        decimal? TargetWeight,
        bool IsActive,
        DateTime StartDate,
        DateTime? EndDate,
        decimal? ProteinGoal,
        decimal? FatGoal,
        decimal? CarbGoal,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}