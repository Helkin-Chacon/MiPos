using Microsoft.AspNetCore.Diagnostics;
using MiPos.Api.Responses;
using MiPos.Application.Common.Exceptions;

namespace MiPos.Api.Errors;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (!httpContext.Request.Path.StartsWithSegments("/api"))
        {
            return false;
        }

        var (status, title, message) = exception switch
        {
            NotFoundException error =>
                (StatusCodes.Status404NotFound, error.Title, error.Message),
            ConflictException error =>
                (StatusCodes.Status409Conflict, error.Title, error.Message),
            BusinessValidationException error =>
                (StatusCodes.Status422UnprocessableEntity, error.Title, error.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor",
                "Ocurrió un error inesperado al procesar la solicitud.")
        };

        var apiError = ApiError.FromRequest(httpContext, title, message);
        if (status == StatusCodes.Status500InternalServerError)
        {
            // Exception messages can contain credentials or database details.
            logger.LogError("Unexpected exception {ExceptionType}. TraceId: {TraceId}",
                exception.GetType().FullName, apiError.TraceId);
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ApiResponse<object>(null, apiError), cancellationToken);

        return true;
    }
}
