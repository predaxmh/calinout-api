using API_Calinout_Project.Entities;
using System.ComponentModel.DataAnnotations;

namespace API_Calinout_Project.DTOs
{
    public class CreateMealRequest
    {

        [Required, MaxLength(200)]
        public required string Name { get; set; }

        public bool IsTemplate { get; set; } = false;

        public DateTime? ConsumedAt { get; set; }

        public List<int>? FoodIds { get; set; }
        public List<CreateFoodRequest>? Foods { get; set; }
    }

    public class UpdateMealRequest
    {

        [MaxLength(200)]
        public string? Name { get; set; }

        public bool? IsTemplate { get; set; }

        public DateTime? ConsumedAt { get; set; }

        public List<int>? FoodIds { get; set; }
    }


    public record MealResponse(
        int Id,
        string UserId,
        string Name,
        bool IsTemplate,
        DateTime? ConsumedAt,
        decimal TotalCalories,
        decimal TotalCarbs,
        decimal TotalProtein,
        decimal TotalFat,
        decimal TotalWeight,
        List<FoodResponse> Foods,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}