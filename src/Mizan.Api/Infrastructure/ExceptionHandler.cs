using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Mizan.Application.Common;
using Mizan.Domain.Common;

namespace Mizan.Api.Infrastructure;

// Turns known exceptions into RFC 7807 Problem Details. Anything else is a 500 without internal details.
internal sealed class ExceptionHandler(IProblemDetailsService problemDetails, ILogger<ExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, "Business rule violated"),
            ValidationException => (StatusCodes.Status400BadRequest, "Invalid request"),
            BadHttpRequestException bad => (bad.StatusCode, "Invalid request"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Authentication failed"),
            _ => (StatusCodes.Status500InternalServerError, "Server error"),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message,
            },
        });
    }
}
