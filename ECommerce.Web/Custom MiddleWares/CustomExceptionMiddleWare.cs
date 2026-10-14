using Ecommerce.Domain.Exceptions;
using Ecommerce.Shared.Common.ErrorModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ECommerce.Web.Custom_MiddleWares
{
    public class CustomExceptionMiddleWare
    {
        private readonly RequestDelegate _next;

        public CustomExceptionMiddleWare(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, ILogger<CustomExceptionMiddleWare> logger)
        {
            try
            {
                await _next.Invoke(context);
                if (context.Response.StatusCode == StatusCodes.Status404NotFound)
                {
                    var response = new ErrorToReturn()
                    {
                        statusCode = StatusCodes.Status404NotFound,
                        Message = $"Endpoint {context.Request.Path} was not found"
                    };
                    await context.Response.WriteAsJsonAsync(response);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Message}", ex.Message);

                var response = new ErrorToReturn()
                {
                    Message = ex.Message
                };

                context.Response.StatusCode = ex switch
                {
                    NotFoundException => StatusCodes.Status404NotFound,
                    UnAuthorizedException => StatusCodes.Status401Unauthorized,
                    BadRequestException badRequest => GetBadRequestErrors(badRequest, response),
                    _ => StatusCodes.Status500InternalServerError
                };

                response.statusCode = context.Response.StatusCode;
                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(response);
            }
        }

        private static int GetBadRequestErrors(BadRequestException exception, ErrorToReturn response)
        {
            response.Errors = exception.Errors;
            return StatusCodes.Status400BadRequest;
        }
    }
}
