using API_Calinout_Project.DTOs;
using API_Calinout_Project.Shared;

namespace API_Calinout_Project.Services.Interfaces.Features
{
    public interface IDailyLogService
    {
        Task<Result<DailyLogResponse>> CreateAsync(string userId, CreateDailyLogRequest request);
        Task<Result<DailyLogResponse>> GetByIdAsync(string userId, int id);
        Task<Result<DailyLogResponse>> GetByDateAsync(string userId, DateTime date);
        Task<Result<PagedResponse<DailyLogResponse>>> GetUserLogsAsync(string userId, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50);
        Task<Result<List<DailyLogResponse>>> GetUserDailyLogByDateRangeAsync(string userId, DateTime? from = null, DateTime? to = null);
        Task<Result<bool>> UpdateAsync(string userId, DateTime date, UpdateDailyLogRequest request);
        Task<Result<bool>> DeleteAsync(string userId, int id);
    }
}