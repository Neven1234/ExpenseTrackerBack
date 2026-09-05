using ExpenseTracker.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Api.Middleware;

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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client gave up on the request; there is nobody left to write a response to.
            _logger.LogInformation("Request to {Path} was cancelled by the client.", context.Request.Path);
        }
        catch (Exception exception)
        {
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = Resolve(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception while processing {Path}", context.Request.Path);

        // Headers are already on the wire, so the status code can no longer be changed.
        if (context.Response.HasStarted)
        {
            _logger.LogWarning(exception, "Response for {Path} had already started; no problem details written.", context.Request.Path);
            return;
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static (int StatusCode, string Title) Resolve(Exception exception) => exception switch
    {
        NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
        ConflictException => (StatusCodes.Status409Conflict, exception.Message),
        InvalidCredentialsException => (StatusCodes.Status401Unauthorized, exception.Message),
        DomainException => (StatusCodes.Status400BadRequest, exception.Message),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, exception.Message),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
    };
}
