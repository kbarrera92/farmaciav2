using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pharmacy.Core.Common;

namespace Pharmacy.Api.Errors;

/// <summary>Translates exceptions into Problem Details responses. Unexpected errors are logged and hidden.</summary>
public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException ve => new ValidationProblemDetails(ve.Errors.ToDictionary(e => e.Key, e => e.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = ve.Message,
            },
            NotFoundException => Create(StatusCodes.Status404NotFound, "No encontrado", exception.Message),
            ConflictException => Create(StatusCodes.Status409Conflict, "Conflicto", exception.Message),
            BusinessRuleException => Create(StatusCodes.Status422UnprocessableEntity, "Operación no permitida", exception.Message),
            AuthenticationFailedException => Create(StatusCodes.Status401Unauthorized, "No autenticado", exception.Message),
            _ => Create(StatusCodes.Status500InternalServerError, "Error interno",
                "Ocurrió un error inesperado. Intente nuevamente o contacte al administrador."),
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Create(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };
}
