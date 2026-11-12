using Banking.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Api.Exceptions;

public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = Map(exception);
        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Title = title,
            Status = status,
            Detail = detail
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
    }

    private static (int Status, string Title, string Detail) Map(Exception exception) => exception switch
    {
        CustomerNotFoundException or AccountNotFoundException or CustomerAccountNotFoundException =>
            (StatusCodes.Status404NotFound, "No encontrado", exception.Message),
        BusinessException =>
            (StatusCodes.Status400BadRequest, "Solicitud no válida", exception.Message),
        _ =>
            (StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado.")
    };
}
