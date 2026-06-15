using System.Text.Json;

namespace Lex.Api.Common;

// Red de seguridad: traduce excepciones a respuestas JSON con el codigo correcto
// y mensaje en espanol. Evita que cualquier fallo termine en un 500 crudo.
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, mensaje) = ex switch
            {
                BadRequestException => (StatusCodes.Status400BadRequest, ex.Message),
                NotFoundException => (StatusCodes.Status404NotFound, ex.Message),
                ForbiddenException => (StatusCodes.Status403Forbidden, ex.Message),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, ex.Message),
                _ => (StatusCodes.Status500InternalServerError, "Ocurrio un error inesperado.")
            };

            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Error no controlado en {Path}", context.Request.Path);

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { error = mensaje }));
        }
    }
}
