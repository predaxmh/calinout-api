using API_Calinout_Project.DTOs;
using API_Calinout_Project.Extensions;
using API_Calinout_Project.Services.Interfaces.Features;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API_Calinout_Project.Controllers.V1
{
    [EnableRateLimiting("UserRequestsLimit")]
    [Authorize]
    [ApiController]
    [Route("api/V1/[controller]")]
    [Produces("application/json")]
    public class FoodTypesController : BaseApiController
    {
        private readonly IFoodTypeService _service;

        public FoodTypesController(IFoodTypeService foodTypeService)
        {
            _service = foodTypeService;
        }

        /// <summary>
        /// Creates a new food type.
        /// </summary>
        /// <param name="request">The creation DTO.</param>
        /// <returns>The created item.</returns>
        /// <response code="201">Returns the newly created item.</response>
        /// <response code="400">If the input model is invalid (Automatic).</response>


        [ProducesResponseType(typeof(FoodTypeResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateFoodTypeRequest request)
        {
            var userId = User.GetUserId();

            var result = await _service.CreateAsync(request, userId);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves a single food type by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the food type.</param>
        /// <returns>The requested food type.</returns>
        /// <response code="200">Returns the item.</response>
        /// <response code="404">If the item is not found.</response>

        [ProducesResponseType(typeof(FoodTypeResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = User.GetUserId();

            var result = await _service.GetByIdAsync(userId, id);

            return HandleResult(result);
        }


        /// <summary>
        /// Retrieves a paginated list of food types.
        /// </summary>
        /// <param name="search">Optional search term for filtering by name.</param>
        /// <param name="page">Page number (default 1).</param>
        /// <param name="pageSize">Items per page (default 50).</param>
        /// <returns>A paged list of food types.</returns>
        /// <response code="200">Returns the list of items.</response>
        /// <response code="400">If page or pageSize are invalid.</response>


        [ProducesResponseType(typeof(PagedResponse<FoodTypeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var userId = User.GetUserId();

            var result = await _service.GetAllAsync(userId, search, page, pageSize);

            return HandleResult(result);
        }


        /// <summary>
        /// Updates an existing food type.
        /// </summary>
        /// <param name="id">The ID of the item to update.</param>
        /// <param name="request">The update DTO.</param>
        /// <response code="204">If the update was successful (No content returned).</response>
        /// <response code="400">If the ID in the URL does not match the ID in the body.</response>
        /// <response code="404">If the item to update does not exist.</response>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateFoodTypeRequest request)
        {
            var userId = User.GetUserId();

            var result = await _service.UpdateAsync(userId, id, request);

            if (result.IsSuccess)
            {
                return NoContent();
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Deletes a food type.
        /// </summary>
        /// <param name="id">The ID of the item to delete.</param>
        /// <response code="204">If the deletion was successful.</response>
        /// <response code="404">If the item was not found.</response>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();

            var result = await _service.DeleteAsync(userId, id);

            if (result.IsSuccess)
            {
                return NoContent();
            }

            return HandleResult(result);
        }
    }
}