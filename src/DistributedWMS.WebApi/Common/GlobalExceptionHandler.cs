using DistributedWMS.Application.Common.Exceptions;
using DistributedWMS.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DistributedWMS.WebApi.Common;

/// <summary>
/// Tłumaczy wyjątki warstw niższych na odpowiedzi HTTP zgodnie ze standardem
/// Problem Details (RFC 7807). Dzięki temu kontrolery nie zawierają obsługi bgłedów.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var problem = exception switch
        {
            NotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Nie odnaleziono zasobu",
                Detail = exception.Message
            },
            DomainException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Naruszenie reguły biznesowej",
                Detail = exception.Message
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Błąd wewnętrzny serwera",
                Detail = "Wystąpił nieoczekiwany błąd podczas przetwarzania żądania."
            }
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Nieobsłużony wyjątek: {Message}", exception.Message);
        else
            _logger.LogWarning("Odrzucono żądanie: {Message}", exception.Message);

        problem.Instance = context.Request.Path;
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        await context.Response.WriteAsJsonAsync(problem, ct);

        return true;
    }
}