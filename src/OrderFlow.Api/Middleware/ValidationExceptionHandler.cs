using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace OrderFlow.Api.Middleware;

public sealed class ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        logger.LogWarning(
            "Validation failed for {HttpMethod} {RequestPath}: {ValidationErrors}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            string.Join("; ", validationException.Errors.Select(error => error.ErrorMessage)));

        var problemDetails = new ValidationProblemDetails(
            validationException.Errors
                .GroupBy(error => error.PropertyName, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).ToArray()))
        {
            Type = "https://tools.ietf.org/html/rfc9457#section-3.2",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
