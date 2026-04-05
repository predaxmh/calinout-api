using API_Calinout_Project.DTOs;
using API_Calinout_Project.Shared;

namespace API_Calinout_Project.Services.Interfaces.Features
{
    public interface IMealService
    {
        Task<Result<MealResponse>> CreateAsync(string userId, CreateMealRequest request);
        Task<Result<MealResponse>> GetByIdAsync(string userId, int id);
        Task<Result<PagedResponse<MealResponse>>> GetUserMealsAsync(string userId, DateTime? from = null, DateTime? to = null, bool? isTemplate = null, int page = 1, int pageSize = 50);
        Task<Result<List<MealResponse>>> GetUserMealsByDateRangeAsync(string userId, DateTime? from = null, DateTime? to = null, bool? isTemplate = null);
        Task<Result<bool>> UpdateAsync(string userId, int id, UpdateMealRequest request);
        //Task<Result<bool>> AddFoodToMealAsync(string userId, int mealId, int foodId);
        //Task<Result<bool>> RemoveFoodFromMealAsync(string userId, int mealId, int foodId);
        Task<Result<bool>> DeleteAsync(string userId, int id);
    }
}