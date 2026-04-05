using API_Calinout_Project.DTOs;
using API_Calinout_Project.Shared;

namespace API_Calinout_Project.Services.Interfaces.Features
{
    public interface IFoodService
    {
        Task<Result<FoodResponse>> CreateAsync(string userId, CreateFoodRequest request);
        Task<Result<FoodResponse>> GetByIdAsync(string userId, int id);
        Task<Result<PagedResponse<FoodResponse>>> GetUserFoodsAsync(string userId, DateTime? from = null, DateTime? to = null, bool? isTemplate = null, bool? withMealId = false, int page = 1, int pageSize = 50);
        Task<Result<List<FoodResponse>>> GetUserFoodsByDateRangeAsync(string userId, DateTime? from = null, DateTime? to = null, bool? isTemplate = null, bool? showFoodInsideMeal = null);
        Task<Result<bool>> UpdateAsync(string userId, int id, UpdateFoodRequest request);
        Task<Result<bool>> DeleteAsync(string userId, int id);
    }
}