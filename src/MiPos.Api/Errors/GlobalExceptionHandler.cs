using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiPos.Application.Common.Exceptions;

namespace MiPos.Api.Errors;

public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
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

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        if (status == StatusCodes.Status500InternalServerError)
        {
            // Exception messages can contain credentials or database details.
            logger.LogError("Unexpected exception {ExceptionType}. TraceId: {TraceId}",
                exception.GetType().FullName, traceId);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = $"{httpContext.Request.PathBase}{httpContext.Request.Path}"
        };
        problem.Extensions["traceId"] = traceId;

        httpContext.Response.StatusCode = status;
        if (!await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        }))
        {
            await httpContext.Response.WriteAsJsonAsync(problem, options: null,
                contentType: "application/problem+json", cancellationToken: cancellationToken);
        }

        return true;
    }
}
