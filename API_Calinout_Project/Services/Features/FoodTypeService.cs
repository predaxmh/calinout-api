using API_Calinout_Project.Data;
using API_Calinout_Project.DTOs;
using API_Calinout_Project.Entities;
using API_Calinout_Project.Extensions;
using API_Calinout_Project.Services.Interfaces.Features;
using API_Calinout_Project.Shared;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace API_Calinout_Project.Services.Features;


public class FoodTypeService : IFoodTypeService
{
    private readonly ApplicationDbContext _context;


    public FoodTypeService(ApplicationDbContext context)
    {

        _context = context;
    }

    public async Task<Result<FoodTypeResponse>> CreateAsync(CreateFoodTypeRequest request, string? userId)
    {

        if (userId is null) return Result<FoodTypeResponse>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);

        // AsNoTracking() 
        bool exists = await _context.FoodTypes
            .AsNoTracking()
            .AnyAsync(x => x.Name.ToLower() == request.Name.ToLower());
        if (exists)
        {
            return Result<FoodTypeResponse>.Failure("Food type with this name already exists", ErrorType.Conflict);
        }

        var foodType = request.Adapt<FoodType>();
        foodType.UserId = userId;

        _context.FoodTypes.Add(foodType);
        await _context.SaveChangesAsync();

        var response = foodType.Adapt<FoodTypeResponse>();

        return Result<FoodTypeResponse>.Success(response);
    }

    public async Task<Result<FoodTypeResponse>> GetByIdAsync(string userId, int id)
    {

        var foodType = await _context.FoodTypes.FindAsync(id);
        if (foodType == null)
            return Result<FoodTypeResponse>.Failure("Food type not found", ErrorType.NotFound);

        if (foodType.UserId != userId)
            return Result<FoodTypeResponse>.Failure("Access denied", ErrorType.Forbidden);

        var response = foodType.Adapt<FoodTypeResponse>();

        return Result<FoodTypeResponse>.Success(response);
    }

    public async Task<Result<PagedResponse<FoodTypeResponse>>> GetAllAsync(string? userId, string? search = null, int page = 1, int pageSize = 50)
    {

        if (userId is null) return Result<PagedResponse<FoodTypeResponse>>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);

        var query = _context.FoodTypes.AsNoTracking().Where(ft => ft.UserId == userId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(ft => ft.Name.Contains(cleanSearch));
        }

        var result = await query
            .OrderBy(ft => ft.Name)
            .ToPagedResponseAsync(page, pageSize, (foodtype) => foodtype.Adapt<FoodTypeResponse>());

        return Result<PagedResponse<FoodTypeResponse>>.Success(result);
    }

    public async Task<Result<bool>> UpdateAsync(string? userId, int id, UpdateFoodTypeRequest request)
    {
        if (userId is null) return Result<bool>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);

        var foodType = await _context.FoodTypes.FindAsync(id);
        if (foodType == null)
            return Result<bool>.Failure("Food type not found", ErrorType.NotFound);

        // Check name uniqueness if changing
        if (request.Name != null && request.Name != foodType.Name)
        {
            if (await _context.FoodTypes.AsNoTracking().AnyAsync(ft => ft.Name == request.Name))
                return Result<bool>.Failure("Food type with this name already exists", ErrorType.Conflict);
        }

        request.Adapt(foodType);

        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAsync(string? userId, int id)
    {
        if (userId is null) return Result<bool>.Failure("Unauthorized Access (no userId) ", ErrorType.Unauthorized);

        var foodType = await _context.FoodTypes.FindAsync(id);
        if (foodType == null)
            return Result<bool>.Failure("Food type not found", ErrorType.NotFound);

        if (await _context.Foods.AsNoTracking().AnyAsync(f => f.FoodTypeId == id))
            return Result<bool>.Failure("Cannot delete food type that is in use", ErrorType.Forbidden);

        _context.FoodTypes.Remove(foodType);
        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

}