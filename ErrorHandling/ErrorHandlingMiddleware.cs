using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RestApi.ErrorHandling;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestApi.ErrorHandling
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); // Continue processing the request
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "An error occurred while processing the request.");

                // Set the status code and response content
                context.Response.StatusCode = 500; // Default to 500 for general errors
                context.Response.ContentType = "application/json";

                // Create an ApiErrorResponse directly
                var errorResponse = new ApiErrorResponse(
                    status: 500,
                    message: "Internal Server Error",
                    detail: ex.Message,
                    instance: context.Request.Path,
                    errors: new List<string> { "An unexpected error occurred. Please try again later." }
                );

                // Return the error response
                await context.Response.WriteAsJsonAsync(errorResponse);
            }
        }
    }
}
