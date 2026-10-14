using Ecommerce.Domain.Exceptions;
using Ecommerce.Shared.Common.ErrorModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ECommerce.Web.Custom_MiddleWares
{
    /// <summary>
    /// Global exception handler middleware.
    /// Maps domain exceptions to appropriate HTTP status codes
    /// and returns a consistent JSON error envelope.
    /// </summary>
    public class CustomExceptionMiddleWare(RequestDelegate next)
    {
        public async Task Invoke(HttpContext context, ILogger<CustomExceptionMiddleWare> logger)
        {
            try
            {
                await next(context);

                if (context.Response.StatusCode == StatusCodes.Status404NotFound)
                {
                    var notFoundResponse = new ErrorToReturn
                    {
                        statusCode = StatusCodes.Status404NotFound,
                        Message    = $"Endpoint '{context.Request.Path}' was not found."
                    };
                    await context.Response.WriteAsJsonAsync(notFoundResponse);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

                var errorResponse = new ErrorToReturn { Message = ex.Message };

                context.Response.StatusCode = ex switch
                {
                    NotFoundException       => StatusCodes.Status404NotFound,
                    UnAuthorizedException   => StatusCodes.Status401Unauthorized,
                    BadRequestException bre => ResolveBadRequest(bre, errorResponse),
                    _                       => StatusCodes.Status500InternalServerError
                };

                errorResponse.statusCode   = context.Response.StatusCode;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(errorResponse);
            }
        }

        private static int ResolveBadRequest(BadRequestException exception, ErrorToReturn response)
        {
            response.Errors = exception.Errors;
            return StatusCodes.Status400BadRequest;
        }
    }
}
