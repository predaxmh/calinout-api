using API_Calinout_Project.Data;
using API_Calinout_Project.DTOs;
using API_Calinout_Project.Entities;
using API_Calinout_Project.Services.Interfaces.Features;
using API_Calinout_Project.Shared;

namespace API_Calinout_Project.Services.Features
{
    public class UserProfileService : IUserProfileService
    {
        private readonly ApplicationDbContext _context;

        public UserProfileService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result<UserProfileResponseDto>> GetById(string userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return Result<UserProfileResponseDto>.Failure("user not found", ErrorType.NotFound);
            }

            DateTime? bithDate = null;
            if (user.DateOfBirth.HasValue)
            {
                bithDate = user.DateOfBirth?.ToDateTime(TimeOnly.MinValue);
            }

            int? gender = user.Gender.HasValue ? (int)user.Gender.Value : 0;



            UserProfileResponseDto userProfile = new UserProfileResponseDto
                (
                 user.Id,
                 user.FirstName,
                 user.LastName,
                 gender,
                 user.HeightInCm,
                 user.WeightInKg,
                 user.MeasurementSystem,
                 user.Email,
                 bithDate
                );
            return Result<UserProfileResponseDto>.Success(userProfile);
        }

        public async Task<Result<bool>> Update(string userId, UpdateUserProfileRequestDto request)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return Result<bool>.Failure("user not found", ErrorType.NotFound);
            }

            if (request.FirstName != null) user.FirstName = request.FirstName;
            if (request.LastName != null) user.LastName = request.LastName;
            if (request.HeightInCm != null) user.HeightInCm = request.HeightInCm;
            if (request.WeightInKg != null) user.WeightInKg = request.WeightInKg;
            if (request.MeasurementSystem != null) user.MeasurementSystem = request.MeasurementSystem;

            if (request.BirthDate.HasValue)
            {
                user.DateOfBirth = DateOnly.FromDateTime(request.BirthDate.Value);
            }
            if (request.Gender.HasValue)
            {
                user.Gender = request.Gender.Value == 0 ? Gender.Male : Gender.Female;
            }


            await _context.SaveChangesAsync();

            return Result<bool>.Success(true);
        }


    }
}