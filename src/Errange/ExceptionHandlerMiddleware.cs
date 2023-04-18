using Errange.Extensions;
using Errange.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Errange;

public class ExceptionHandlerMiddleware
{
    private readonly ILogger<ExceptionHandlerMiddleware> _logger;
    private readonly RequestDelegate _next;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An exception has been occurred.");

            await HandleException(context, exception);
        }
    }

    public async Task HandleException(HttpContext httpContext, Exception exception)
    {
        Type exceptionType = exception.GetType();
        _logger.LogTrace("Handling '{ExceptionTypeFullName}' exception started.", exceptionType.FullName);

        PrepareHttpContextForExceptionHandling(httpContext, exception);
        PathString originalPath = httpContext.Request.Path;

        var errangeOptions = httpContext.RequestServices.GetRequiredService<ErrangeOptions>();
        ProblemPolicy problemPolicy = errangeOptions.GetPolicyForNearestExceptionType(exceptionType);

        _logger.LogTrace("Generating error from '{ExceptionTypeFullName}' exception via error policy for '{ErrorPolicyExceptionTypeFullName}' exception.", exceptionType.FullName, problemPolicy.ExceptionType.FullName);
        ErrangeProblemDetails problemDetails = problemPolicy.CreateProblemDetails(
            exception,
            httpContext,
            httpContext.RequestServices,
            errangeOptions.ExceptionInfoInclusionPredicate(httpContext.RequestServices.GetRequiredService<IHostEnvironment>()));
        _logger.LogTrace("Writing error from '{ExceptionTypeFullName}' exception to response.", exceptionType.FullName);

        await httpContext.WriteObjectResult(problemDetails.Status, problemDetails);
        httpContext.Request.Path = originalPath;
    }

    private void PrepareHttpContextForExceptionHandling(HttpContext httpContext, Exception exception)
    {
        if (httpContext.Response.HasStarted)
        {
            _logger.LogDebug("Can't handle exception, because response has already been started.");
            throw exception;
        }

        httpContext
            .ClearHttpContext()
            .SetExceptionHandlerFeatures(exception);
    }
}