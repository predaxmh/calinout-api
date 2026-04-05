using System.ComponentModel.DataAnnotations;

namespace API_Calinout_Project.Entities
{
    /// <summary>
    /// Represents a meal entry, including nutritional information, consumption details, and associated foods for a
    /// user.
    /// </summary>
    /// <remarks>A meal can be either a consumed meal or a reusable template, as indicated by the IsTemplate
    /// property. Nutritional totals are typically the sum of the associated foods. The Foods collection contains the
    /// individual food items that make up the meal. This class is intended for use in applications that track dietary
    /// intake or meal planning.</remarks>
    public class Meal
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;
        [Required, MaxLength(200)]
        public string Name { get; set; } = null!; 
        public bool IsTemplate { get; set; }     
        public DateTime? ConsumedAt { get; set; } 

        
        public decimal TotalCalories { get; set; }
        public decimal TotalCarbs { get; set; }
        public decimal TotalProtein { get; set; }
        public decimal TotalFat { get; set; }
        public decimal TotalWeight { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<Food> Foods { get; set; } = new HashSet<Food>();
        public ApplicationUser User { get; set; } = null!;
    }
}