namespace API_Calinout_Project.Entities
{
    /// <summary>
    /// Represents a type of food, including its nutritional information and metadata.
    /// </summary>
    /// <remarks>This class is typically used to define reusable food templates or categories, such as "Chicken
    /// Breast" or "Brown Rice", with associated macronutrient values per a specified base weight. Instances of this class
    /// can be associated with multiple food entries consumed by a user. Thread safety is not guaranteed for instances of
    /// this class.</remarks>
    public class FoodType
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public decimal Calories { get; set; }
        public decimal Protein { get; set; }
        public decimal Fat { get; set; }
        public decimal Carbs { get; set; }
        public decimal BaseWeightInGrams { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<Food> Foods { get; set; } = new HashSet<Food>();
    }
}