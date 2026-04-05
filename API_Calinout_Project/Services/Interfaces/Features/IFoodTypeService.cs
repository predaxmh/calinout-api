using API_Calinout_Project.DTOs;
using API_Calinout_Project.Shared;

namespace API_Calinout_Project.Services.Interfaces.Features
{
    public interface IFoodTypeService
    {
        Task<Result<FoodTypeResponse>> CreateAsync(CreateFoodTypeRequest request, string? userId);
        Task<Result<FoodTypeResponse>> GetByIdAsync(string userId, int id);
        Task<Result<PagedResponse<FoodTypeResponse>>> GetAllAsync(string? userId, string? search = null, int page = 1, int pageSize = 50);
        Task<Result<bool>> UpdateAsync(string? userId, int id, UpdateFoodTypeRequest request);
        Task<Result<bool>> DeleteAsync(string? userId, int id);
    }
}