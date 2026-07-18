using System.Text.Json;
using AuthService.Exceptions;

namespace AuthService.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleException(context, ex);
        }
    }

    private static async Task HandleException(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ConflictException => new
            {
                StatusCode = StatusCodes.Status409Conflict,
                exception.Message
            },

            UnauthorizedAccessException => new
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                exception.Message
            },

            _ => new
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Message = "Ocorreu um erro interno."
            }
        };

        context.Response.StatusCode = response.StatusCode;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}