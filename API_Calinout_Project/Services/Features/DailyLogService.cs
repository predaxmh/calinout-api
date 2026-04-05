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
    public class DailyLogService : IDailyLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DailyLogService> _logger;
        public DailyLogService(ApplicationDbContext context, ILogger<DailyLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Result<DailyLogResponse>> CreateAsync(string userId, CreateDailyLogRequest request)
        {

            var exist = _context.DailyLogs.AsNoTracking().Any(daily => daily.UserId == userId && daily.Date == request.Date);
            if (exist)
            {
                _logger.LogWarning("A already created the daily log for the date {userId} ", userId);
                return Result<DailyLogResponse>.Failure("A already created the daily log for the date.", ErrorType.Conflict);
            }

            var dailyLog = request.Adapt<DailyLog>();


            _context.DailyLogs.Add(dailyLog);
            await _context.SaveChangesAsync();


            var response = dailyLog.Adapt<DailyLogResponse>();

            return Result<DailyLogResponse>.Success(response);
        }


        public async Task<Result<DailyLogResponse>> GetByIdAsync(string userId, int id)
        {
            var dailyLog = await _context.DailyLogs.FindAsync(id);
            if (dailyLog == null)
                return Result<DailyLogResponse>.Failure("Daily log not found", ErrorType.NotFound);

            if (dailyLog.UserId != userId)
                return Result<DailyLogResponse>.Failure("Access denied", ErrorType.Forbidden);

            var response = dailyLog.Adapt<DailyLogResponse>();

            return Result<DailyLogResponse>.Success(response);
        }

        public async Task<Result<DailyLogResponse>> GetByDateAsync(string userId, DateTime date)
        {
            var dailyLog = await _context.DailyLogs
                .FirstOrDefaultAsync(dl => dl.UserId == userId && dl.Date.Date == date.Date);

            bool ignoreFirstTime = false;

            if (dailyLog == null)
            {
                ignoreFirstTime = true;
                dailyLog = new DailyLog()
                {
                    UserId = userId,
                    Date = date,
                    CreatedAt = DateTime.UtcNow,
                };

                _context.DailyLogs.Add(dailyLog);

            }

            if (!ignoreFirstTime)
            {

                var foods = await _context.Foods.AsNoTracking().Where(food => food.UserId == userId && food.IsTemplate == false && food.MealId == null && food.ConsumedAt >= date && food.ConsumedAt < date.AddDays(1))
                              .Select(food => new
                              {
                                  totalFat = food.Fat,
                                  totalProtein = food.Protein,
                                  totalCarb = food.Carbs,
                                  totalCalories = food.Calories,
                                  totalWeight = food.WeightInGrams,

                              }).GroupBy(x => 1)
                              .Select(g => new
                              {
                                  SumFat = g.Sum(x => x.totalFat),
                                  SumProtein = g.Sum(x => x.totalProtein),
                                  SumCarb = g.Sum(x => x.totalCarb),
                                  SumCalories = g.Sum(x => x.totalCalories),
                                  SumWeight = g.Sum(x => x.totalWeight),

                              })
                              .OrderBy(x => 1)
                              .FirstOrDefaultAsync();

                var meals = await _context.Meals.AsNoTracking().Where(meal => meal.UserId == userId && meal.IsTemplate == false && meal.ConsumedAt >= date && meal.ConsumedAt < date.AddDays(1))
                        .Select(meal => new
                        {
                            totalFat = meal.TotalFat,
                            totalProtein = meal.TotalProtein,
                            totalCarb = meal.TotalCarbs,
                            totalCalories = meal.TotalCalories,
                            totalWeight = meal.TotalWeight,

                        }).GroupBy(x => 1)
                        .Select(g => new
                        {
                            SumFat = g.Sum(x => x.totalFat),
                            SumProtein = g.Sum(x => x.totalProtein),
                            SumCarb = g.Sum(x => x.totalCarb),
                            SumCalories = g.Sum(x => x.totalCalories),
                            SumWeight = g.Sum(x => x.totalWeight),

                        })
                        .OrderBy(x => 1)
                        .FirstOrDefaultAsync();

                var totalCalories = (meals?.SumCalories ?? 0) + (foods?.SumCalories ?? 0);
                var totalFat = (foods?.SumFat ?? 0) + (meals?.SumFat ?? 0);
                var totalCab = (foods?.SumCarb ?? 0) + (meals?.SumCarb ?? 0);
                var totalProtein = (foods?.SumProtein ?? 0) + (meals?.SumProtein ?? 0);
                var totalWeight = (foods?.SumWeight ?? 0) + (meals?.SumWeight ?? 0);



                dailyLog.TotalCalories = (int)totalCalories;
                dailyLog.TotalFat = totalFat;
                dailyLog.TotalProtein = totalProtein;
                dailyLog.TotalFoodWeight = totalWeight;
                dailyLog.TotalCarbs = totalCab;

            }


            await _context.SaveChangesAsync();

            var response = dailyLog.Adapt<DailyLogResponse>();


            return Result<DailyLogResponse>.Success(response);
        }


        public async Task<Result<List<DailyLogResponse>>> GetUserDailyLogByDateRangeAsync(
            string userId,
            DateTime? from = null,
            DateTime? to = null
            )
        {
            if (userId is null) return Result<List<DailyLogResponse>>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);


            var query = _context.DailyLogs.AsNoTracking()

            .Where(d => d.UserId == userId && d.WeightAtLog != null);

            if (from.HasValue)
                query = query.Where(d => d.Date >= from.Value);

            if (to.HasValue)
                query = query.Where(d => d.Date <= to.Value);

            var result = await query
                .OrderByDescending(m => m.Date)
                .ProjectToType<DailyLogResponse>()
                .ToListAsync();

            return Result<List<DailyLogResponse>>.Success(result);

        }

        public async Task<Result<PagedResponse<DailyLogResponse>>> GetUserLogsAsync(
            string userId,
            DateTime? from = null,
            DateTime? to = null,
            int page = 1,
            int pageSize = 50)
        {

            var query = _context.DailyLogs.AsNoTracking().Where(dl => dl.UserId == userId);

            if (from.HasValue)
                query = query.Where(dl => dl.Date >= from.Value.Date);

            if (to.HasValue)
                query = query.Where(dl => dl.Date <= to.Value.Date);

            var dailyLogs = await query
                .OrderByDescending(dl => dl.Date)
                .ToPagedResponseAsync(page, pageSize, (dailyLog) => dailyLog.Adapt<DailyLogResponse>());

            return Result<PagedResponse<DailyLogResponse>>.Success(dailyLogs);


        }

        public async Task<Result<bool>> UpdateAsync(string userId, DateTime date, UpdateDailyLogRequest request)
        {


            var dailyLog = await _context.DailyLogs.FirstOrDefaultAsync(daily => daily.UserId == userId && daily.Date == date);

            if (dailyLog == null)
                return Result<bool>.Failure("Daily log not found", ErrorType.NotFound);



            if (request.BurnedCalories.HasValue) dailyLog.BurnedCalories = request.BurnedCalories;
            if (request.WeightAtLog.HasValue) dailyLog.WeightAtLog = request.WeightAtLog;
            if (request.DigestiveTrackCleared.HasValue) dailyLog.DigestiveTrackCleared = request.DigestiveTrackCleared.Value;
            if (request.IsCheatDay.HasValue) dailyLog.IsCheatDay = request.IsCheatDay.Value;
            if (request.TargetCalorieOnThisDay.HasValue) dailyLog.TargetCalorieOnThisDay = request.TargetCalorieOnThisDay;
            if (request.DailyNotes != null) dailyLog.DailyNotes = request.DailyNotes;


            dailyLog.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }


        public async Task<Result<bool>> DeleteAsync(string userId, int id)
        {
            var dailyLog = await _context.DailyLogs.FindAsync(id);
            if (dailyLog == null)
                return Result<bool>.Failure("Daily log not found", ErrorType.NotFound);

            if (dailyLog.UserId != userId)
                return Result<bool>.Failure("Access denied", ErrorType.Forbidden);


            _context.DailyLogs.Remove(dailyLog);
            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);

        }

    }
}