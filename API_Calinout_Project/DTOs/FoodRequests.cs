using System.ComponentModel.DataAnnotations;

namespace API_Calinout_Project.DTOs
{
    public class CreateFoodRequest
    {
        [Required]
        public int FoodTypeId { get; set; }

        public int? MealId { get; set; }

        [Range(0.01, 99999.99)]
        public decimal WeightInGrams { get; set; }

        public bool IsTemplate { get; set; } = false;

        public DateTime? ConsumedAt { get; set; }
    }

    public class UpdateFoodRequest
    {

        [Range(0.01, 99999.99)]
        public decimal? WeightInGrams { get; set; }

        public DateTime? ConsumedAt { get; set; }

        public int? MealId { get; set; }
    }

    public record FoodResponse(
        int Id,
        string UserId,
        int FoodTypeId,
        string Name,
        int? MealId,
        decimal WeightInGrams,
        DateTime? ConsumedAt,
        bool IsTemplate,
        decimal Calories,
        decimal Protein,
        decimal Fat,
        decimal Carbs,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );
}