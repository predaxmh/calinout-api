using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net;
using System.Security.Authentication;

namespace API_Calinout_Project.Infrastructure.Exceptions
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // 1. Log the full error natively
            _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

            // 2. Map the exception to an HTTP Status Code and standardized message
            var (statusCode, title, detail) = MapException(exception);

            // 3. Append inner exception details ONLY if in Development
            if (_env.IsDevelopment() && exception.InnerException != null)
            {
                detail = $"{detail} Inner Exception: {exception.InnerException.Message}";
            }

            // 4. Create the Problem Details response
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            };

            // Add a trace ID so front-end devs can give you a code to look up in your logs
            var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
            if (traceId != null)
            {
                problemDetails.Extensions["traceId"] = traceId;
            }

            // 5. Write the response
            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }

        private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
        {
            return exception switch
            {
                // --- Database Conflicts & Errors ---
                DbUpdateConcurrencyException => (
                    (int)HttpStatusCode.Conflict,
                    "Concurrency Conflict",
                    "The record was modified by another user. Please refresh and try again."
                ),
                DbUpdateException => (
                    (int)HttpStatusCode.Conflict,
                    "Database Conflict",
                    "A database conflict occurred. This is likely due to a foreign key constraint or duplicate data."
                ),

                // --- Not Found ---
                KeyNotFoundException => (
                    (int)HttpStatusCode.NotFound,
                    "Resource Not Found",
                    "The requested resource could not be found."
                ),

                // --- Bad Requests / Validation ---
                ArgumentNullException or ArgumentException => (
                    (int)HttpStatusCode.BadRequest,
                    "Invalid Request",
                    exception.Message // Usually safe to show argument exceptions
                ),
                InvalidOperationException => (
                    (int)HttpStatusCode.BadRequest,
                    "Invalid Operation",
                    "The requested operation cannot be performed in the current state."
                ),

                // --- Auth & Permissions ---
                UnauthorizedAccessException => (
                    (int)HttpStatusCode.Forbidden,
                    "Access Denied",
                    "You do not have permission to access this resource."
                ),
                AuthenticationException => (
                    (int)HttpStatusCode.Unauthorized,
                    "Unauthorized",
                    "Authentication failed or token is invalid."
                ),

                // --- Fallback ---
                _ => (
                    (int)HttpStatusCode.InternalServerError,
                    "Internal Server Error",
                    "An unexpected error occurred. Please contact support if the issue persists."
                )
            };
        }
    }
}