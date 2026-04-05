using API_Calinout_Project.Data;
using API_Calinout_Project.DTOs;
using API_Calinout_Project.Entities;
using API_Calinout_Project.Extensions;
using API_Calinout_Project.Services.Interfaces.Features;
using API_Calinout_Project.Shared;
using Mapster;
using Microsoft.EntityFrameworkCore;


namespace API_Calinout_Project.Services.Features
{

    public class FoodService : IFoodService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IFoodService> _logger;

        public FoodService(ApplicationDbContext context, ILogger<IFoodService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<FoodResponse>> CreateAsync(string userId, CreateFoodRequest request)
        {
            if (userId is null) return Result<FoodResponse>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);

            var foodType = await _context.FoodTypes.FindAsync(request.FoodTypeId);
            if (foodType == null)
                return Result<FoodResponse>.Failure("Food type not found", ErrorType.NotFound);

            // If MealId provided, verify ownership
            if (request.MealId.HasValue)
            {
                var meal = await _context.Meals.FindAsync(request.MealId.Value);
                if (meal == null || meal.UserId != userId)

                    return Result<FoodResponse>.Failure("Meal inside the Food type not found or access denied", ErrorType.NotFound);
            }

            var (cal, protein, fat, carbs) = CalculateMacros(foodType, request.WeightInGrams);

            var food = new Food
            {
                UserId = userId,
                FoodTypeId = request.FoodTypeId,
                FoodType = foodType,
                MealId = request.MealId,
                Name = foodType.Name,
                WeightInGrams = request.WeightInGrams,
                ConsumedAt = request.ConsumedAt,
                IsTemplate = request.IsTemplate,
                Calories = cal,
                Protein = protein,
                Fat = fat,
                Carbs = carbs,
                CreatedAt = DateTime.UtcNow
            };

            _context.Foods.Add(food);
            await _context.SaveChangesAsync();


            var response = food.Adapt<FoodResponse>();

            return Result<FoodResponse>.Success(response);
        }

        public async Task<Result<FoodResponse>> GetByIdAsync(string userId, int id)
        {

            if (userId is null) return Result<FoodResponse>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);

            var food = await _context.Foods
                .Include(f => f.FoodType)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (food == null)
                return Result<FoodResponse>.Failure("Food not found", ErrorType.NotFound);

            if (food.UserId != userId)
                return Result<FoodResponse>.Failure("Access denied", ErrorType.Forbidden);

            var response = food.Adapt<FoodResponse>();

            return Result<FoodResponse>.Success(response);
        }

        public async Task<Result<PagedResponse<FoodResponse>>> GetUserFoodsAsync(
            string userId,
            DateTime? from = null,
            DateTime? to = null,
            bool? isTemplate = null,
            bool? withMealId = null,
            int page = 1,
            int pageSize = 50)
        {

            if (userId is null) return Result<PagedResponse<FoodResponse>>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);
            var query = _context.Foods.AsNoTracking()

            .Where(f => f.UserId == userId);

            if (from.HasValue)
                query = query.Where(f => f.ConsumedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(f => f.ConsumedAt <= to.Value);

            if (isTemplate.HasValue)
                query = query.Where(f => f.IsTemplate == isTemplate.Value);

            if (withMealId.HasValue)
            {
                if (withMealId.Value == false)
                    query = query.Where(f => f.MealId == null);
            }

            var foods = await query
                .OrderByDescending(f => f.ConsumedAt)
                .ToPagedResponseAsync(page, pageSize, (food) => food.Adapt<FoodResponse>());

            return Result<PagedResponse<FoodResponse>>.Success(foods);
        }


        public async Task<Result<List<FoodResponse>>> GetUserFoodsByDateRangeAsync(
            string userId,
            DateTime? from = null,
            DateTime? to = null,
            bool? isTemplate = null,
            bool? showFoodInsideMeal = null
            )

        {
            if (userId is null) return Result<List<FoodResponse>>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);


            var query = _context.Foods.AsNoTracking()

            .Where(f => f.UserId == userId);

            if (from.HasValue)
                query = query.Where(f => f.ConsumedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(f => f.ConsumedAt <= to.Value);

            if (isTemplate.HasValue)
                query = query.Where(f => f.IsTemplate == isTemplate.Value);

            if (showFoodInsideMeal.HasValue)
            {
                if (showFoodInsideMeal.Value == false)
                    query = query.Where(f => f.MealId == null);
            }


            var result = await query
                .OrderByDescending(m => m.ConsumedAt ?? DateTime.MaxValue)
                .ProjectToType<FoodResponse>()
                .ToListAsync();

            return Result<List<FoodResponse>>.Success(result);
        }


        public async Task<Result<bool>> UpdateAsync(string userId, int id, UpdateFoodRequest request)
        {

            if (userId is null) return Result<bool>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);
            var food = await _context.Foods
                .Include(f => f.FoodType)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (food == null)
                return Result<bool>.Failure("Food not found", ErrorType.NotFound);

            if (food.UserId != userId)
                return Result<bool>.Failure("Access denied", ErrorType.Forbidden);

            // If weight changed, recalculate nutritional values
            if (request.WeightInGrams.HasValue && request.WeightInGrams.Value != food.WeightInGrams)
            {
                var ratio = request.WeightInGrams.Value / food.FoodType.BaseWeightInGrams;
                food.WeightInGrams = request.WeightInGrams.Value;
                food.Calories = food.FoodType.Calories * ratio;
                food.Protein = food.FoodType.Protein * ratio;
                food.Fat = food.FoodType.Fat * ratio;
                food.Carbs = food.FoodType.Carbs * ratio;
            }

            if (request.ConsumedAt.HasValue)
                food.ConsumedAt = request.ConsumedAt.Value;

            if (request.MealId.HasValue)
            {
                var meal = await _context.Meals.FindAsync(request.MealId.Value);
                if (meal == null || meal.UserId != userId)
                    return Result<bool>.Failure("Meal not found or access denied", ErrorType.Forbidden);

                food.MealId = request.MealId.Value;
            }

            food.UpdatedAt = DateTime.UtcNow;


            await _context.SaveChangesAsync();
            return Result<bool>.Success(true);

        }

        public async Task<Result<bool>> DeleteAsync(string userId, int id)
        {
            if (userId is null) return Result<bool>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);


            var food = await _context.Foods.FindAsync(id);
            if (food == null)
                return Result<bool>.Failure("Food not found", ErrorType.NotFound);

            if (food.UserId != userId)
                return Result<bool>.Failure("Access denied", ErrorType.Forbidden);


            _context.Foods.Remove(food);
            await _context.SaveChangesAsync();
            return Result<bool>.Success(true);

        }


        public static (decimal cal, decimal protein, decimal fat, decimal carbs)
        CalculateMacros(FoodType foodType, decimal weightInGrams)
        {
            var ratio = weightInGrams / foodType.BaseWeightInGrams;
            return (
                foodType.Calories * ratio,
                foodType.Protein * ratio,
                foodType.Fat * ratio,
                foodType.Carbs * ratio
            );
        }
    }
}