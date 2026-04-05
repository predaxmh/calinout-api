using API_Calinout_Project.Entities;
using API_Calinout_Project.Services.Features;
using FluentAssertions;

namespace API_Calinout.Tests.Unit.Services.Features;

// Why a separate class?
// CalculateMacros is a pure function — no DB, no logger, no setup needed.
// Mixing it into FoodServiceTests would force it to share InMemory DB overhead
// it doesn't need. Separate class = faster, cleaner, focused.
public class FoodMacroCalculationTests
{
    // ── Test 7: Macros scale proportionally ─────────────────────────────────
    // What: 200g of a food with 100g base weight should double all macros.
    // Why: the core business logic of the entire nutrition tracking feature.
    // If this is wrong, every food log in the app has wrong values.
    [Fact]
    public void CalculateMacros_DoubleWeight_DoublesMacros()
    {
        // Arrange
        var foodType = new FoodType
        {
            BaseWeightInGrams = 100,
            Calories = 200,
            Protein = 20,
            Fat = 10,
            Carbs = 5
        };

        // Act
        var result = FoodService.CalculateMacros(foodType, 200);

        // Assert
        result.cal.Should().Be(400);
        result.protein.Should().Be(40);
        result.fat.Should().Be(20);
        result.carbs.Should().Be(10);
    }

    // ── Test 8: Partial weight ────────────────────────────────────────────────
    // What: 50g should halve all macros.
    // Why: tests the ratio going below 1, not just above.
    // Edge cases on both sides of 1.0 are important for ratio-based math.
    [Fact]
    public void CalculateMacros_HalfWeight_HalvesMacros()
    {
        // Arrange
        var foodType = new FoodType
        {
            BaseWeightInGrams = 100,
            Calories = 200,
            Protein = 20,
            Fat = 10,
            Carbs = 5
        };

        // Act
        var result = FoodService.CalculateMacros(foodType, 50);

        // Assert
        result.cal.Should().Be(100);
        result.protein.Should().Be(10);
        result.fat.Should().Be(5);
        result.carbs.Should().Be(2.5m);
    }
}