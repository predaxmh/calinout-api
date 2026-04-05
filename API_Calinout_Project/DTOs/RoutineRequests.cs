using System.ComponentModel.DataAnnotations;

namespace API_Calinout_Project.DTOs
{
    public class CreateRoutineRequest
    {
        public int? TemplateMealId { get; set; }

        public int? TemplateFoodId { get; set; }

        [Required]
        public TimeSpan TargetTime { get; set; }

        public bool IsEnabled { get; set; } = true;
    }

    public class UpdateRoutineRequest
    {
        public int? TemplateMealId { get; set; }

        public int? TemplateFoodId { get; set; }

        public TimeSpan? TargetTime { get; set; }

        public bool? IsEnabled { get; set; }
    }

    public record RoutineResponse(
        int Id,
        string UserId,
        int? TemplateMealId,
        string? TemplateMealName,
        int? TemplateFoodId,
        string? TemplateFoodName,
        TimeSpan TargetTime,
        bool IsEnabled,
        DateTime? LastRunDate,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}