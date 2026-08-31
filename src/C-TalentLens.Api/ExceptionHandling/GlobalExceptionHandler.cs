using C_TalentLens.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace C_TalentLens.Api;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ApiException apiException => CreateProblemDetails(httpContext, apiException),
            UnauthorizedAccessException unauthorized => CreateProblemDetails(
                httpContext,
                StatusCodes.Status403Forbidden,
                "Forbidden.",
                unauthorized.Message,
                "forbidden"),
            _ => CreateUnhandledProblemDetails(httpContext, exception)
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Handled API exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static ProblemDetails CreateProblemDetails(HttpContext context, ApiException exception)
    {
        return CreateProblemDetails(
            context,
            exception.StatusCode,
            exception.Title,
            exception.Message,
            exception.ErrorCode);
    }

    private ProblemDetails CreateUnhandledProblemDetails(HttpContext context, Exception exception)
    {
        var detail = environment.IsDevelopment() || environment.IsEnvironment("Testing")
            ? exception.Message
            : "An unexpected error occurred while processing the request.";

        return CreateProblemDetails(
            context,
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.",
            detail,
            "internal_server_error");
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string errorCode)
    {
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;
        problem.Extensions["errorCode"] = errorCode;

        return problem;
    }
}
