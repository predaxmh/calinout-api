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

    public class MealsController : BaseApiController
    {
        private readonly IMealService _mealService;
        private readonly ILogger<IMealService> _logger;

        public MealsController(IMealService mealService, ILogger<IMealService> logger)
        {
            _mealService = mealService;
            _logger = logger;

        }

        /// <summary>
        /// Creates a new meal using the specified request data.
        /// </summary>
        /// <param name="request">The details of the meal to create. Must not be null.</param>
        /// <returns>A 201 Created response containing the created meal if successful; otherwise, a 400 Bad Request response if
        /// the request is invalid.</returns>
        [ProducesResponseType(typeof(MealResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateMealRequest request)
        {
            var userId = User.GetUserId();

            var result = await _mealService.CreateAsync(userId, request);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves the details of a meal by its unique identifier for the current user.
        /// </summary>
        /// <param name="id">The unique identifier of the meal to retrieve.</param>
        /// <returns>An <see cref="IActionResult"/> containing a <see cref="MealResponse"/> with the meal details if found;
        /// otherwise, a 404 Not Found result.</returns>
        [ProducesResponseType(typeof(MealResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = User.GetUserId();
            var result = await _mealService.GetByIdAsync(userId, id);
            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves the list of meals for the current user within the specified date range and template filter.
        /// </summary>
        /// <param name="from">The start date of the range to retrieve meals for. If null, no lower bound is applied.</param>
        /// <param name="to">The end date of the range to retrieve meals for. If null, no upper bound is applied.</param>
        /// <param name="isTemplate">If set to <see langword="true"/>, only template meals are returned; if <see langword="false"/>, only
        /// non-template meals are returned; if null, both types are included.</param>
        /// <returns>An <see cref="IActionResult"/> containing a list of meals matching the specified criteria with status code
        /// 200 OK, or status code 400 Bad Request if the request is invalid.</returns>
        [ProducesResponseType(typeof(List<MealResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("GetUserMealsByDateRange")]
        public async Task<IActionResult> GetUserMeals(

            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] bool? isTemplate)

        {
            var userId = User.GetUserId();
            var result = await _mealService.GetUserMealsByDateRangeAsync(userId, from, to, isTemplate);
            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves a paged list of meals for the current user, optionally filtered by date range and template status.
        /// </summary>
        /// <param name="from">The start date of the range to filter meals by. Only meals created on or after this date are included. If
        /// null, no lower date bound is applied.</param>
        /// <param name="to">The end date of the range to filter meals by. Only meals created on or before this date are included. If
        /// null, no upper date bound is applied.</param>
        /// <param name="isTemplate">A value indicating whether to include only template meals (<see langword="true"/>), only non-template meals
        /// (<see langword="false"/>), or all meals (null).</param>
        /// <param name="page">The page number of results to return. Must be greater than 0. Defaults to 1.</param>
        /// <param name="pageSize">The maximum number of meals to include in a single page of results. Must be greater than 0. Defaults to 50.</param>
        /// <returns>An <see cref="IActionResult"/> containing a <see cref="PagedResponse{MealResponse}"/> with the user's meals
        /// that match the specified filters. Returns a 200 OK response with the paged results, or a 400 Bad Request if
        /// the parameters are invalid.</returns>
        [ProducesResponseType(typeof(PagedResponse<MealResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("GetUserMeals")]
        public async Task<IActionResult> GetUserMeals(

            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] bool? isTemplate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var userId = User.GetUserId();
            var result = await _mealService.GetUserMealsAsync(userId, from, to, isTemplate, page, pageSize);
            return HandleResult(result);
        }

        /// <summary>
        /// Updates the details of an existing meal for the authenticated user.
        /// </summary>
        /// <param name="id">The unique identifier of the meal to update.</param>
        /// <param name="request">An object containing the updated meal information. Cannot be null.</param>
        /// <returns>A 204 No Content response if the update is successful; otherwise, a 400 Bad Request or 404 Not Found
        /// response depending on the error condition.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMealRequest request)
        {
            var userId = User.GetUserId();
            var result = await _mealService.UpdateAsync(userId, id, request);
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return HandleResult(result);
        }
        /// <summary>
        /// Deletes the meal with the specified identifier for the current user.
        /// </summary>
        /// <param name="id">The unique identifier of the meal to delete.</param>
        /// <returns>A 204 No Content response if the meal was successfully deleted; otherwise, a 404 Not Found response if the
        /// meal does not exist or does not belong to the current user.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();
            var result = await _mealService.DeleteAsync(userId, id);
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return HandleResult(result);
        }
    }
}