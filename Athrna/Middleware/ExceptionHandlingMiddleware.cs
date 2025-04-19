using System.Net;
using System.Text.Json;

namespace Athrna.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "text/html";

            // Log the exception
            _logger.LogError(exception, "An unhandled exception occurred");

            // Get the original path that caused the exception
            var path = context.Request.Path;
            var method = context.Request.Method;
            _logger.LogError($"Exception occurred on {method} {path}");

            var response = context.Response;
            var statusCode = (int)HttpStatusCode.InternalServerError;
            var message = "An unexpected error occurred. Please try again later.";

            // Customize the status code and message based on the exception type
            if (exception is ArgumentException)
            {
                statusCode = (int)HttpStatusCode.BadRequest;
                message = "Invalid input provided. Please check your data and try again.";
            }
            else if (exception is UnauthorizedAccessException)
            {
                statusCode = (int)HttpStatusCode.Unauthorized;
                message = "You are not authorized to perform this action.";
            }
            else if (exception is InvalidOperationException)
            {
                statusCode = (int)HttpStatusCode.BadRequest;
                message = "Invalid operation. Please try again with different parameters.";
            }

            response.StatusCode = statusCode;

            // For API calls, return JSON
            if (context.Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
                return;
            }

            // For web requests, redirect to custom error page with details
            context.Items["OriginalPath"] = path;
            context.Items["ErrorMessage"] = message;
            context.Items["ErrorCode"] = statusCode;
            context.Items["ErrorDetails"] = exception.Message;

            // Redirect to custom error page
            context.Response.Redirect($"/Home/Error?statusCode={statusCode}&message={Uri.EscapeDataString(message)}");
        }
    }
}
