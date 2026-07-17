using System.Text.Json;

namespace SalesService.Middleware;

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
            KeyNotFoundException => new
            {
                StatusCode = StatusCodes.Status404NotFound,
                exception.Message
            },

            ArgumentException => new
            {
                StatusCode = StatusCodes.Status400BadRequest,
                exception.Message
            },

            InvalidOperationException => new
            {
                StatusCode = StatusCodes.Status400BadRequest,
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