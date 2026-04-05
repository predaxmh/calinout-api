using API_Calinout_Project.DTOs;
using API_Calinout_Project.Shared;

namespace API_Calinout_Project.Services.Interfaces.Features
{
    public interface IUserProfileService
    {
        public Task<Result<UserProfileResponseDto>> GetById(string userId);
        public Task<Result<bool>> Update(string userId, UpdateUserProfileRequestDto request);
    }
}