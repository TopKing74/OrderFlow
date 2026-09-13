using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Common.Exceptions;

namespace OrderFlow.Api.Middleware;

public sealed class NotFoundExceptionHandler(ILogger<NotFoundExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException notFoundException)
        {
            return false;
        }

        logger.LogInformation(
            "Resource not found: {ResourceName} with key '{Key}' ({HttpMethod} {RequestPath})",
            notFoundException.Name,
            notFoundException.Key,
            httpContext.Request.Method,
            httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc9457#section-3.2",
            Title = "The specified resource was not found.",
            Detail = notFoundException.Message,
            Status = StatusCodes.Status404NotFound,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
