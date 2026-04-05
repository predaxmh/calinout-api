namespace API_Calinout_Project.Entities
{
    /// <summary>
    /// Represents a food log entry recorded by a user.
    /// A food can be standalone or associated with a <see cref="Meal"/>.
    /// When <see cref="IsTemplate"/> is true, it serves as a reusable entry.
    /// </summary>
    public class Food
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int FoodTypeId { get; set; }
        public int? MealId { get; set; }

        public decimal WeightInGrams { get; set; }
        public DateTime? ConsumedAt { get; set; }

        public bool IsTemplate { get; set; }

        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Fat { get; set; }
        public decimal Carbs { get; set; }


        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public FoodType FoodType { get; set; } = null!;
        public Meal? Meal { get; set; }
        public ApplicationUser User { get; set; } = null!;
    }
}