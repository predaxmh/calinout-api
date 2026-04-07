using API_Calinout_Project.DTOs;
using API_Calinout_Project.Extensions;
using API_Calinout_Project.Services.Features;
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

    public class DailyLogsController : BaseApiController
    {
        private readonly IDailyLogService _dailyLogService;


        public DailyLogsController(IDailyLogService dailyLogService)
        {
            _dailyLogService = dailyLogService;

        }

        /// <summary>
        /// Creates a new daily log entry for the authenticated user.
        /// </summary>
        /// <remarks>The authenticated user's identity is determined from the current request context. The
        /// created resource can be retrieved using the URI provided in the response.</remarks>
        /// <param name="request">The details of the daily log to create. Cannot be null.</param>
        /// <returns>A 201 Created response containing the created daily log if successful; otherwise, a 400 Bad Request response
        /// with error details.</returns>
        [ProducesResponseType(typeof(DailyLogResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDailyLogRequest request)
        {
            var userId = User.GetUserId();
            var result = await _dailyLogService.CreateAsync(userId, request);

            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves a daily log entry for the current user by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the daily log entry to retrieve.</param>
        /// <returns>An <see cref="IActionResult"/> containing a <see cref="DailyLogResponse"/> with the requested daily log
        /// entry if found; otherwise, a 404 Not Found result.</returns>
        [ProducesResponseType(typeof(DailyLogResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = User.GetUserId();
            var result = await _dailyLogService.GetByIdAsync(userId, id);

            return HandleResult(result);
        }
        /// <summary>
        /// Retrieves the daily log entry for the authenticated user on the specified date.
        /// </summary>
        /// <param name="date">The date for which to retrieve the daily log entry. Only the date component is considered; the time
        /// component is ignored.</param>
        /// <returns>An <see cref="IActionResult"/> containing the daily log entry if found; otherwise, a 404 Not Found response.</returns>
        [ProducesResponseType(typeof(DailyLogResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpGet("GetByDate")]
        public async Task<IActionResult> GetByDate([FromQuery] DateTime date)
        {
            var userId = User.GetUserId();
            var result = await _dailyLogService.GetByDateAsync(userId, date);

            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves a paginated list of daily log entries for the current user within the specified date range.
        /// </summary>
        /// <param name="from">The start date of the log entries to retrieve. Only logs on or after this date are included. If null, no
        /// lower date limit is applied.</param>
        /// <param name="to">The end date of the log entries to retrieve. Only logs on or before this date are included. If null, no
        /// upper date limit is applied.</param>
        /// <param name="page">The page number of results to return. Must be greater than or equal to 1. Defaults to 1.</param>
        /// <param name="pageSize">The maximum number of log entries to include in a single page of results. Must be greater than 0. Defaults
        /// to 50.</param>
        /// <returns>An <see cref="IActionResult"/> containing a <see cref="PagedResponse{DailyLogResponse}"/> with the user's
        /// daily logs if successful; otherwise, a Bad Request response if the parameters are invalid.</returns>
        [ProducesResponseType(typeof(PagedResponse<DailyLogResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet]
        public async Task<IActionResult> GetUserLogs(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var userId = User.GetUserId();
            var result = await _dailyLogService.GetUserLogsAsync(userId, from, to, page, pageSize);

            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves a paged list of daily log entries for the current user within the specified date range.
        /// </summary>
        /// <param name="from">The start date of the range to retrieve daily logs for. If null, no lower bound is applied.</param>
        /// <param name="to">The end date of the range to retrieve daily logs for. If null, no upper bound is applied.</param>
        /// <returns>An <see cref="IActionResult"/> containing a <see cref="PagedResponse{DailyLogResponse}"/> with the user's
        /// daily logs for the specified date range if successful; otherwise, a response indicating the error.</returns>
        [ProducesResponseType(typeof(PagedResponse<DailyLogResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpGet("GetUserDailyLogRange")]
        public async Task<IActionResult> GetUserDailyLogRange(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            var userId = User.GetUserId();
            var result = await _dailyLogService.GetUserDailyLogByDateRangeAsync(userId, from, to);

            return HandleResult(result);
        }
        /// <summary>
        /// Updates the daily log entry for the specified date with the provided information.
        /// </summary>
        /// <param name="date">The date of the daily log entry to update.</param>
        /// <param name="request">The updated daily log data to apply. Cannot be null.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the update operation. Returns 204 No Content if the
        /// update is successful; 400 Bad Request if the request is invalid; or 404 Not Found if the daily log entry
        /// does not exist.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpPut]
        public async Task<IActionResult> Update(DateTime date, [FromBody] UpdateDailyLogRequest request)
        {
            var userId = User.GetUserId();
            var result = await _dailyLogService.UpdateAsync(userId, date, request);
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Deletes the daily log entry with the specified identifier for the current user. 
        /// </summary>
        /// <param name="id">The unique identifier of the daily log entry to delete.</param>
        /// <returns>A 204 No Content response if the deletion is successful; otherwise, a 404 Not Found response if the entry
        /// does not exist or cannot be deleted.</returns>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.GetUserId();
            var result = await _dailyLogService.DeleteAsync(userId, id);
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return HandleResult(result);
        }
    }
}