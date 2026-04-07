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

    public class FoodsController : BaseApiController
    {
        private readonly IFoodService _foodService;

        public FoodsController(IFoodService foodService)
        {
            _foodService = foodService;
        }


        /// <summary>
        /// Creates a new food log entry for the authenticated user.
        /// </summary>
        /// <param name="request">The food creation request containing food type, weight, and optional meal association.</param>
        /// <returns>The created food entry with calculated macros.</returns>
        [ProducesResponseType(typeof(FoodResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost]

        public async Task<IActionResult> Create([FromBody] CreateFoodRequest request)
        {
            var userId = User.GetUserId();

            var result = await _foodService.CreateAsync(userId, request);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves a food log entry by ID. Only accessible by the owning user.
        /// </summary>
        [ProducesResponseType(typeof(FoodResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = User.GetUserId();
            var result = await _foodService.GetByIdAsync(userId, id);
            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves a paginated list of foods associated with the current user, optionally filtered by date range, template
        /// status, and meal association.
        /// </summary>
        /// <param name="from">The start date and time for filtering foods. Only foods created on or after this date are included. If null, no
        /// lower date bound is applied.</param>
        /// <param name="to">The end date and time for filtering foods. Only foods created on or before this date are included. If null, no upper
        /// date bound is applied.</param>
        /// <param name="isTemplate">If set to <see langword="true"/>, only foods marked as templates are included; if <see langword="false"/>, only
        /// non-template foods are included. If null, both types are included.</param>
        /// <param name="withMealId">If set to <see langword="true"/>, only foods associated with a meal are included; if <see langword="false"/>, only
        /// foods not associated with a meal are included. If null, both are included.</param>
        /// <param name="page">The page number of the results to return. Must be greater than or equal to 1. Defaults to 1.</param>
        /// <param name="pageSize">The maximum number of items to include in a single page of results. Must be greater than 0. Defaults to 50.</param>
        /// <returns>An <see cref="IActionResult"/> containing a <see cref="PagedResponse{FoodResponse}"/> with the user's foods that
        /// match the specified filters. Returns a 200 OK response with the paged data, or a 400 Bad Request if the parameters
        /// are invalid.</returns>
        [ProducesResponseType(typeof(PagedResponse<FoodResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("GetUserFoods")]
        public async Task<IActionResult> GetUserFoods(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] bool? isTemplate,
            [FromQuery] bool? withMealId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var userId = User.GetUserId();
            var result = await _foodService.GetUserFoodsAsync(userId, from, to, isTemplate, withMealId, page, pageSize);

            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves a list of foods logged by the current user within the specified date range.
        /// </summary>
        /// <param name="from">The start date of the range to filter foods by. If null, no lower bound is applied.</param>
        /// <param name="to">The end date of the range to filter foods by. If null, no upper bound is applied.</param>
        /// <param name="isTemplate">Indicates whether to include only template foods. If null, both template and non-template foods are
        /// included.</param>
        /// <param name="showFoodInsideMeal">Indicates whether to include foods that are part of a meal. If null, all foods are included regardless of
        /// meal association.</param>
        /// <returns>An <see cref="IActionResult"/> containing a list of foods matching the specified criteria with status code
        /// 200 (OK), or a 400 (Bad Request) if the input parameters are invalid.</returns>
        [ProducesResponseType(typeof(List<FoodResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("GetUserFoodsByDateRange")]
        public async Task<IActionResult> GetUserFoodsByRangeAsync(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] bool? isTemplate,
            [FromQuery] bool? showFoodInsideMeal
            )

        {
            var userId = User.GetUserId();
            var result = await _foodService.GetUserFoodsByDateRangeAsync(userId, from, to, isTemplate, showFoodInsideMeal);

            return HandleResult(result);
        }

        /// <summary>
        /// Updates the details of an existing food item with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the food item to update.</param>
        /// <param name="request">The updated values for the food item. Must not be null.</param>
        /// <returns>A 204 No Content response if the update is successful; otherwise, a 400 Bad Request or 404 Not Found
        /// response depending on the error condition.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateFoodRequest request)
        {
            var userId = User.GetUserId();
            var result = await _foodService.UpdateAsync(userId, id, request);
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return HandleResult(result);
        }
        /// <summary>
        /// Deletes the food item with the specified identifier for the current user.
        /// </summary>
        /// <param name="id">The unique identifier of the food item to delete.</param>
        /// <returns>A 204 No Content response if the deletion is successful; otherwise, a 404 Not Found response if the item
        /// does not exist or does not belong to the current user.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();
            var result = await _foodService.DeleteAsync(userId, id);

            if (result.IsSuccess)
            {
                return NoContent();
            }

            return HandleResult(result);
        }
    }
}