using System.ComponentModel.DataAnnotations;

namespace API_Calinout_Project.DTOs
{

    public record FoodTypeResponse(
       int Id,
       string Name,
       decimal Calories,
       decimal Protein,
       decimal Fat,
       decimal Carbs,
       decimal BaseWeightInGrams,
       DateTime CreatedAt,
       DateTime? UpdatedAt
   );


    public class CreateFoodTypeRequest
    {
        [Required, MaxLength(200)]
        public required string Name { get; set; }

        [Range(0, 99999.99)]
        public decimal Calories { get; set; }

        [Range(0, 99999.99)]
        public decimal Protein { get; set; }

        [Range(0, 99999.99)]
        public decimal Fat { get; set; }

        [Range(0, 99999.99)]
        public decimal Carbs { get; set; }

        [Range(0.01, 99999.99)]
        public decimal BaseWeightInGrams { get; set; }
    }

    public class UpdateFoodTypeRequest
    {
        public int Id { get; set; }

        [MaxLength(200)]
        public string? Name { get; set; }

        [Range(0, 99999.99)]
        public decimal? Calories { get; set; }

        [Range(0, 99999.99)]
        public decimal? Protein { get; set; }

        [Range(0, 99999.99)]
        public decimal? Fat { get; set; }

        [Range(0, 99999.99)]
        public decimal? Carbs { get; set; }

        [Range(0, 99999.99)]
        public decimal? BaseWeightInGrams { get; set; }
    }
}