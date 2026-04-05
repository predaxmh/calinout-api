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
    public class MealService : IMealService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FoodTypeService> _logger;


        public MealService(ApplicationDbContext context, ILogger<FoodTypeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<MealResponse>> CreateAsync(string userId, CreateMealRequest request)
        {

            if (userId is null) return Result<MealResponse>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        Meal meal = new Meal
                        {
                            UserId = userId,
                            Name = request.Name,
                            IsTemplate = request.IsTemplate,
                            ConsumedAt = request.ConsumedAt,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Meals.Add(meal);

                        await _context.SaveChangesAsync();

                        var config = new TypeAdapterConfig();

                        if (request.Foods != null)
                        {
                            foreach (var foodRequest in request.Foods)
                            {
                                var foodType = await _context.FoodTypes.FindAsync(foodRequest.FoodTypeId);
                                if (foodType == null) throw new Exception("Object is null, rolling back transaction."); ;
                                var ratio = foodRequest.WeightInGrams / foodType.BaseWeightInGrams;


                                config.NewConfig<CreateFoodRequest, Food>()
                                    .MapWith(src => new Food
                                    {
                                        UserId = userId, // Closure: uses local service variable
                                        FoodTypeId = foodType.Id,
                                        FoodType = foodType,
                                        MealId = src.MealId,
                                        Name = foodType.Name,
                                        WeightInGrams = src.WeightInGrams,
                                        ConsumedAt = src.ConsumedAt,
                                        IsTemplate = src.IsTemplate,
                                        Calories = foodType.Calories * ratio,
                                        Protein = foodType.Protein * ratio,
                                        Fat = foodType.Fat * ratio,
                                        Carbs = foodType.Carbs * ratio,
                                        CreatedAt = DateTime.UtcNow
                                    });

                                // 2. Execute the map
                                var food = foodRequest.Adapt<Food>(config);

                                food.MealId = meal.Id;
                                _context.Foods.Add(food);

                            }
                        }

                        await _context.SaveChangesAsync();

                        await RecalculateMealTotalsAsync(meal.Id);

                        await _context.Entry(meal).Collection(m => m.Foods).Query()
                            .Include(f => f.FoodType)
                            .LoadAsync();

                        transaction.Commit();

                        return Result<MealResponse>.Success(MapToResponse(meal));
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.LogWarning("failed to create meal" + ex.Message);
                        return Result<MealResponse>.Failure("failed to create meal", ErrorType.Server);
                    }

                }
            });
        }

        public async Task<Result<MealResponse>> GetByIdAsync(string userId, int id)
        {
            if (userId is null) return Result<MealResponse>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);

            var meal = await _context.Meals

                .FirstOrDefaultAsync(m => m.Id == id);

            if (meal == null)
                return Result<MealResponse>.Failure("Meal not found", ErrorType.NotFound);

            if (meal.UserId != userId)
                return Result<MealResponse>.Failure("Access denied", ErrorType.Forbidden);

            return Result<MealResponse>.Success(MapToResponse(meal));
        }

        public async Task<Result<PagedResponse<MealResponse>>> GetUserMealsAsync(
            string userId,
            DateTime? from = null,
            DateTime? to = null,
            bool? isTemplate = null,
            int page = 1,
            int pageSize = 10)
        {

            if (userId is null) return Result<PagedResponse<MealResponse>>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);
            var query = _context.Meals
                .Include(m => m.Foods)
                .ThenInclude(f => f.FoodType)
                .Where(m => m.UserId == userId);

            if (from.HasValue && !isTemplate.GetValueOrDefault())
                query = query.Where(m => m.ConsumedAt >= from.Value);

            if (to.HasValue && !isTemplate.GetValueOrDefault())
                query = query.Where(m => m.ConsumedAt <= to.Value);

            if (isTemplate.HasValue)
                query = query.Where(m => m.IsTemplate == isTemplate.Value);

            var result = await query
                .OrderByDescending(m => m.ConsumedAt ?? DateTime.MaxValue)
                .ToPagedResponseAsync(page, pageSize, (meal) => meal.Adapt<MealResponse>());


            return Result<PagedResponse<MealResponse>>.Success(result);
        }



        public async Task<Result<List<MealResponse>>> GetUserMealsByDateRangeAsync(
            string userId,
            DateTime? from = null,
            DateTime? to = null,
            bool? isTemplate = null)

        {
            if (userId is null) return Result<List<MealResponse>>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);

            var query = _context.Meals.AsNoTracking()
                .Include(m => m.Foods)
                .ThenInclude(f => f.FoodType)
                .Where(m => m.UserId == userId);

            if (from.HasValue && !isTemplate.GetValueOrDefault())
                query = query.Where(m => m.ConsumedAt >= from.Value);

            if (to.HasValue && !isTemplate.GetValueOrDefault())
                query = query.Where(m => m.ConsumedAt <= to.Value);

            if (isTemplate.HasValue)
                query = query.Where(m => m.IsTemplate == isTemplate.Value);

            var result = await query
                .OrderByDescending(m => m.ConsumedAt ?? DateTime.MaxValue)
                .ProjectToType<MealResponse>()
                .ToListAsync();


            return Result<List<MealResponse>>.Success(result);
        }

        public async Task<Result<bool>> UpdateAsync(string userId, int id, UpdateMealRequest request)
        {

            if (userId is null) return Result<bool>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);

            var meal = await _context.Meals
                .Include(m => m.Foods)
                .ThenInclude(f => f.FoodType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (meal == null)
                return Result<bool>.Failure("Meal not found", ErrorType.NotFound);

            if (meal.UserId != userId)
                return Result<bool>.Failure("Access denied", ErrorType.Forbidden);

            if (request.Name != null)
                meal.Name = request.Name;

            if (request.IsTemplate.HasValue)
                meal.IsTemplate = request.IsTemplate.Value;

            if (request.ConsumedAt.HasValue)
                meal.ConsumedAt = request.ConsumedAt.Value;


            meal.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await RecalculateMealTotalsAsync(id);

            // Reload
            await _context.Entry(meal).Collection(m => m.Foods).Query()
                .Include(f => f.FoodType)
                .LoadAsync();

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> DeleteAsync(string userId, int id)
        {
            if (userId is null) return Result<bool>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);

            var meal = await _context.Meals.FindAsync(id);
            if (meal == null)
                return Result<bool>.Failure("Meal not found", ErrorType.NotFound);

            if (meal.UserId != userId)
                return Result<bool>.Failure("Access denied", ErrorType.Forbidden);


            // Remove all current foods from meal
            var currentFoods = await _context.Foods
                .Where(f => f.MealId == id)
                .ToListAsync();

            _context.Foods.RemoveRange(currentFoods);


            _context.Meals.Remove(meal);
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }

        private async Task RecalculateMealTotalsAsync(int mealId)
        {
            var meal = await _context.Meals
                .Include(m => m.Foods)
                .FirstOrDefaultAsync(m => m.Id == mealId);

            if (meal == null) return;

            meal.TotalCalories = meal.Foods.Sum(f => f.Calories);
            meal.TotalCarbs = meal.Foods.Sum(f => f.Carbs);
            meal.TotalProtein = meal.Foods.Sum(f => f.Protein);
            meal.TotalFat = meal.Foods.Sum(f => f.Fat);
            meal.TotalWeight = meal.Foods.Sum(f => f.WeightInGrams);
            meal.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private static MealResponse MapToResponse(Meal meal) => new(
            meal.Id,
            meal.UserId,
            meal.Name,
            meal.IsTemplate,
            meal.ConsumedAt,
            meal.TotalCalories,
            meal.TotalCarbs,
            meal.TotalProtein,
            meal.TotalFat,
            meal.TotalWeight,
            meal.Foods.Select(f => new FoodResponse(
                f.Id,
                f.UserId,
                f.FoodTypeId,
                f.FoodType.Name,
                f.MealId,
                f.WeightInGrams,
                f.ConsumedAt,
                f.IsTemplate,
                f.Calories,
                f.Protein,
                f.Fat,
                f.Carbs,
                f.CreatedAt,
                f.UpdatedAt
            )).ToList(),
            meal.CreatedAt,
            meal.UpdatedAt
        );
    }
}