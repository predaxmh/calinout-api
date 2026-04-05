using API_Calinout_Project.Shared;
using Microsoft.AspNetCore.Mvc;


namespace API_Calinout_Project.Controllers.V1
{
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        // This generic method handles ALL your service results automatically
        protected ActionResult HandleResult<T>(Result<T> result)
        {
            // 1. Success Case
            if (result.IsSuccess)
            {
                if (result.Value == null) return StatusCode(500, new { error = "Unexpected null response" });
                return Ok(result.Value);
            }

            // 2. Failure Cases (Mapping ErrorType -> HTTP Status)
            // Adjust these checks based on how your Result class is structured
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(new { error = result.Error }),
                ErrorType.Conflict => Conflict(new { error = result.Error }), // Returns 409
                ErrorType.Forbidden => Forbid(),
                ErrorType.Validation => BadRequest(new { error = result.Error }),
                ErrorType.Unauthorized => Unauthorized(new { error = result.Error }),
                _ => BadRequest(new { error = result.Error }) // Default to 400
            };
        }
    }
}