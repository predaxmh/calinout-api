using API_Calinout_Project.DTOs;
using API_Calinout_Project.Extensions;
using API_Calinout_Project.Services.Interfaces.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API_Calinout_Project.Controllers.V1
{
    [Authorize]
    [ApiController]
    [Route("api/V1/[controller]")]
    [Produces("application/json")]
    public class UserProfileController : BaseApiController
    {
        private readonly IUserProfileService _userProfileService;

        public UserProfileController(IUserProfileService userProfileService)
        {
            _userProfileService = userProfileService;
        }

        [ProducesResponseType(typeof(UserProfileResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("GetById")]
        public async Task<IActionResult> GetById()
        {
            var userId = User.GetUserId();
            var result = await _userProfileService.GetById(userId);

            return HandleResult(result);
        }

        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPut()]
        public async Task<IActionResult> Update(UpdateUserProfileRequestDto requestDto)
        {
            var userId = User.GetUserId();
            var result = await _userProfileService.Update(userId, requestDto);

            if (result.IsSuccess)
            {
                return NoContent();
            }

            return HandleResult(result);
        }
    }
}